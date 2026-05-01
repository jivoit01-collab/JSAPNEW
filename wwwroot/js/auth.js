(function () {
    if (window.__authInitialized) return;
    window.__authInitialized = true;

    /* ========== FETCH INTERCEPTOR ========== */
    (function () {
        var originalFetch = window.fetch;
        window.fetch = function () {
            var args = Array.prototype.slice.call(arguments);
            var options = args[1] || {};
            options.credentials = options.credentials || "include";
            args[1] = options;
            return originalFetch.apply(this, args).then(function (response) {
                if (response.status === 401) {
                    console.warn("API returned 401 (unauthorized). Check authentication.");
                    // 不要自动重定向 - 让服务器 [Authorize] 处理页面级重定向
                    // 只会影响到 API 调用的错误处理
                }
                return response;
            });
        };
    })();

    /* ========== JQUERY AJAX SETUP ========== */
    function setupJQueryAuth() {
        if (typeof jQuery === "undefined") return;
        var $ = jQuery;

        $.ajaxSetup({
            xhrFields: { withCredentials: true }
        });

        $(document).ajaxError(function (event, xhr, settings, thrownError) {
            if (xhr.status === 0 || thrownError === "abort") return;
            if (xhr.status === 401) {
                console.warn("API returned 401 (unauthorized). Check authentication.");
                // 不要自动重定向 - 让服务器处理
                return;
            }
            if (xhr.status === 403) {
                if (window.APP && window.APP.showError) {
                    window.APP.showError("Access Denied", "You do not have permission to view this resource.");
                }
                return;
            }
            if (xhr.status === 429) {
                if (window.APP && window.APP.showError) {
                    window.APP.showError("Too Many Requests", "Please wait a moment and try again.");
                }
                return;
            }
            var msg = "An unexpected error occurred";
            try {
                var resp = xhr.responseJSON;
                if (resp && resp.message) msg = resp.message;
                else if (resp && resp.Message) msg = resp.Message;
            } catch (e) { }
            if (thrownError && thrownError !== "error") msg = thrownError;
            console.error("AJAX Error:", xhr.status, msg);
            if (window.APP && window.APP.showApiError) {
                window.APP.showApiError(msg);
            }
        });
    }

    /* ========== UI ERROR HELPERS ========== */
    window.APP = window.APP || {};

    window.APP.showError = function (title, message) {
        var overlay = document.getElementById("appErrorOverlay");
        if (!overlay) {
            overlay = document.createElement("div");
            overlay.id = "appErrorOverlay";
            overlay.style.cssText = "position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.5);z-index:99999;display:flex;align-items:center;justify-content:center;";
            overlay.innerHTML = '<div id="appErrorBox" style="background:#fff;border-radius:12px;padding:40px 50px;max-width:450px;width:90%;text-align:center;box-shadow:0 20px 60px rgba(0,0,0,0.3);"><div id="appErrorIcon" style="font-size:48px;margin-bottom:15px;"></div><h3 id="appErrorTitle" style="margin:0 0 10px;color:#1e293b;font-size:20px;"></h3><p id="appErrorMsg" style="margin:0 0 25px;color:#64748b;font-size:14px;line-height:1.5;"></p><button onclick="window.APP.hideError()" style="background:#0b2c4d;color:#fff;border:none;padding:10px 30px;border-radius:8px;font-size:14px;cursor:pointer;font-weight:600;">OK</button></div>';
            document.body.appendChild(overlay);
        }
        document.getElementById("appErrorIcon").textContent = title === "Access Denied" ? "\uD83D\uDD12" : "\u26A0\uFE0F";
        document.getElementById("appErrorTitle").textContent = title;
        document.getElementById("appErrorMsg").textContent = message;
        overlay.style.display = "flex";
    };

    window.APP.showApiError = function (message) {
        var container = document.getElementById("apiErrorContainer");
        if (!container) {
            container = document.createElement("div");
            container.id = "apiErrorContainer";
            container.style.cssText = "background:#fef2f2;border:1px solid #fecaca;border-radius:8px;padding:16px 20px;margin:16px;display:flex;align-items:center;gap:12px;color:#991b1b;font-size:14px;";
            var mainBody = document.getElementById("mainBody");
            if (mainBody && mainBody.firstChild) {
                mainBody.insertBefore(container, mainBody.firstChild);
            } else if (mainBody) {
                mainBody.appendChild(container);
            }
        }
        container.innerHTML = '<span style="font-size:20px;">\u26A0\uFE0F</span><span>' + message.replace(/</g, "&lt;").replace(/>/g, "&gt;") + '</span>';
        container.style.display = "flex";
        setTimeout(function () { container.style.display = "none"; }, 8000);
    };

    window.APP.hideError = function () {
        var overlay = document.getElementById("appErrorOverlay");
        if (overlay) overlay.style.display = "none";
    };

    window.APP.getAuthFetch = function (url, options) {
        options = options || {};
        options.credentials = options.credentials || "include";
        return fetch(url, options);
    };

    /* ========== INIT ========== */
    setupJQueryAuth();

    if (typeof document !== "undefined") {
        document.addEventListener("DOMContentLoaded", function () {
            setupJQueryAuth();
        });
    }

    (function pollJQuery() {
        if (typeof jQuery !== "undefined") {
            setupJQueryAuth();
        } else {
            setTimeout(pollJQuery, 50);
        }
    })();
})();
