# JSAPNEW - Windows Server CI/CD Deployment Guide

## How It Works (Overview)

```
Developer pushes to main branch
        │
        ▼
GitHub detects push → triggers workflow
        │
        ▼
Self-hosted runner (your Windows server) picks up the job
        │
        ▼
Build → Publish → Stop IIS → Copy files → Start IIS
        │
        ▼
App is live with new changes ✓
```

**In simple terms:** You push code to `main` → GitHub tells your Windows server → the server builds the code, stops the website, replaces old files with new ones, and starts the website again. Fully automatic.

---

## Architecture

```
┌──────────────┐       ┌──────────────────┐       ┌─────────────────────┐
│  Developer   │──push─▶│  GitHub (main)   │──job──▶│  Windows Server     │
│  (VS Code)   │       │  Actions trigger  │       │  (Self-Hosted Runner)│
└──────────────┘       └──────────────────┘       │                     │
                                                   │  1. dotnet restore  │
                                                   │  2. dotnet build    │
                                                   │  3. dotnet publish  │
                                                   │  4. Stop IIS site   │
                                                   │  5. Copy files      │
                                                   │  6. Start IIS site  │
                                                   │  7. Health check    │
                                                   └─────────────────────┘
```

---

## Prerequisites on the Windows Server

### 1. Install .NET 10 SDK & Runtime

Download and install from https://dotnet.microsoft.com/download/dotnet/10.0

Verify installation:
```powershell
dotnet --version
```

### 2. Install & Configure IIS

Open PowerShell as Administrator:
```powershell
# Install IIS with required features
Install-WindowsFeature -Name Web-Server, Web-Asp-Net45, Web-Net-Ext45, Web-ISAPI-Ext, Web-ISAPI-Filter, Web-Mgmt-Console, Web-WebSockets

# Install ASP.NET Core Hosting Bundle (REQUIRED for .NET apps on IIS)
# Download from: https://dotnet.microsoft.com/download/dotnet/10.0 → "Hosting Bundle"
```

> **Important:** The **ASP.NET Core Hosting Bundle** is mandatory. Without it, IIS cannot run .NET Core apps. Restart IIS after installing it: `iisreset`

### 3. Create the IIS Website

```powershell
Import-Module WebAdministration

# Create application pool (No Managed Code)
New-WebAppPool -Name "JSAPNEW"
Set-ItemProperty "IIS:\AppPools\JSAPNEW" -Name "managedRuntimeVersion" -Value ""

# Create site directory
New-Item -ItemType Directory -Path "C:\inetpub\wwwroot\JSAPNEW" -Force

# Create website
New-Website -Name "JSAPNEW" -Port 80 -PhysicalPath "C:\inetpub\wwwroot\JSAPNEW" -ApplicationPool "JSAPNEW"
```

> **Note:** Set `managedRuntimeVersion` to empty string (`""`) because ASP.NET Core apps use the out-of-process hosting model, not the classic .NET CLR.

### 4. Place Production appsettings on the Server

Create `C:\inetpub\wwwroot\JSAPNEW\appsettings.json` with your production database connection strings and settings. The deployment pipeline **preserves this file** — it backs it up before deploying and restores it after, so your production secrets are never overwritten by the repo version.

---

## Setting Up the GitHub Actions Self-Hosted Runner

This is the most important step — it connects your Windows server to GitHub so it can receive and execute jobs.

### Step-by-Step:

1. Go to your GitHub repo → **Settings** → **Actions** → **Runners** → **New self-hosted runner**

2. Select **Windows** as the OS

3. On your Windows server, open PowerShell as Administrator and run the commands GitHub gives you:

```powershell
# Create a folder for the runner
mkdir C:\actions-runner && cd C:\actions-runner

# Download the runner (GitHub will show the exact URL)
Invoke-WebRequest -Uri https://github.com/actions/runner/releases/download/v2.XXX.X/actions-runner-win-x64-2.XXX.X.zip -OutFile actions-runner-win-x64.zip

# Extract
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory("$PWD\actions-runner-win-x64.zip", "$PWD")

# Configure (GitHub provides the exact token)
.\config.cmd --url https://github.com/YOUR_ORG/JSAP-web --token YOUR_TOKEN

# Install as Windows Service (so it starts automatically on boot)
.\svc.cmd install
.\svc.cmd start
```

4. Verify the runner appears as **Online** (green) in GitHub → Settings → Actions → Runners

### Runner Service Account Permissions

The runner service account needs:
- **Read/Write** access to `C:\inetpub\wwwroot\JSAPNEW`
- **Permission to manage IIS** (Stop/Start websites) — run the service as a local admin or grant specific IIS permissions

```powershell
# If running as a specific user, grant IIS management permissions
.\svc.cmd install --user "DOMAIN\ServiceAccount" --password "password"
```

---

## Deployment Workflow Explained

Here is what happens at each step when you push to `main`:

| Step | What Happens | Why |
|------|-------------|-----|
| **Checkout** | Pulls latest code from `main` branch | Gets your new changes |
| **Setup .NET** | Ensures .NET 10 SDK is available | Needed to build the project |
| **Restore** | Downloads NuGet packages | Resolves project dependencies |
| **Build** | Compiles the project in Release mode | Creates optimized binaries |
| **Publish** | Creates deployment-ready output in `./publish` | Bundles everything needed to run |
| **Stop IIS** | Stops the website | Releases file locks so files can be replaced |
| **Deploy** | Copies published files to IIS directory | Replaces old application with new version |
| **Start IIS** | Starts the website back up | Makes the app live again |
| **Health Check** | Sends an HTTP request to verify | Confirms the app is running |

---

## How to Deploy

### Automatic Deployment (Normal Flow)
```bash
# Make your changes, then:
git add .
git commit -m "your changes"
git push origin main
```
That's it. The pipeline runs automatically.

### Monitor Deployment
- Go to GitHub repo → **Actions** tab → see the running/completed workflow
- Click on a run to see step-by-step logs
- Green checkmark = success, Red X = failure

---

## Environment Variables to Customize

Update these in [Jsap-Deploye.yml](.github/workflows/Jsap-Deploye.yml) based on your server setup:

| Variable | Default | Description |
|----------|---------|-------------|
| `DOTNET_VERSION` | `10.0.x` | .NET SDK version |
| `APP_NAME` | `JSAPNEW` | Application name |
| `DEPLOY_PATH` | `C:\inetpub\wwwroot\JSAPNEW` | IIS site physical path on server |
| `IIS_SITE_NAME` | `JSAPNEW` | IIS website name |

---

## Branching Strategy

```
feature/xyz  ──▶  deploye-setup (or dev)  ──▶  main (auto-deploys)
   work here        test/review here          production
```

- **Only `main` triggers deployment** — defined in the workflow `on.push.branches`
- Work on feature branches, merge to `main` when ready to deploy
- Use Pull Requests for code review before merging to `main`

---

## Troubleshooting

### Build fails on "dotnet restore"
- Check if the runner has internet access to reach NuGet.org
- Check if any private NuGet feeds are configured in `NuGet.config`

### "Access denied" when copying files
- The runner service account needs write permission to `DEPLOY_PATH`
- Run: `icacls "C:\inetpub\wwwroot\JSAPNEW" /grant "NT AUTHORITY\SYSTEM:(OI)(CI)F"`

### IIS site won't start after deploy
- Check Windows Event Viewer → Application logs
- Run `dotnet JSAPNEW.dll` manually from the deploy folder to see errors
- Verify the ASP.NET Core Hosting Bundle is installed

### "502.5 - Process Failure" in browser
- The Hosting Bundle is missing or wrong version
- Connection string or appsettings issue — check that production config was preserved

### Runner shows "Offline" in GitHub
- Check if the service is running: `Get-Service actions.runner.*`
- Restart it: `Restart-Service actions.runner.*`

---

## Security Notes

- **Never commit production `appsettings.json`** with real connection strings to the repo
- The pipeline automatically preserves server-side `appsettings.json` during deployment
- Consider using GitHub Secrets for sensitive values and injecting them during deploy
- Ensure the runner service account follows the principle of least privilege
