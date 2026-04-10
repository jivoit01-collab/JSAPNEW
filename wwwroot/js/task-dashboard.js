// ============================================
// TASK DASHBOARD JS - Hierarchy Integrated
// ============================================

const API = '/api/Task';
let currentViewMode = 'OWN';
let currentPage = 1;
let teamMembers = [];
let taskRowCount = 1;
let selectedTaskType = 'SELF'; // SELF or ASSIGN
let selectedTimeSlot = null;   // 10AM-1PM | 1PM-4PM | 4PM-7PM
let hierarchyData = null;      // HOD dept tree
let breakdownData = [];        // Per-employee task counts
let dashboardPromise = null;
let allTeamTasks = [];
let employeeProgressMap = {};
let subHODCurrentTab = 'my'; // reused for overview tabs: 'my' | 'team' | 'allhod'
let currentTaskFilterOverrides = {};
let allScopedTeamTasks = [];
let lastOwnTasks = [];
let taskDetailsLookup = {};
let allHodMembers = [];
let allHodTasks = [];
let createTaskInFlight = false;
let progressUpdateInFlight = false;
let reassignInFlight = false;
let taskActionLocks = new Set();

function clearDashboardCache() {
    dashboardPromise = null;
    taskDetailsLookup = {};
}

function getTaskOwnerEmployeeId(task) {
    return getValue(task, 'assignedToEmployeeId', 'AssignedToEmployeeId') || 0;
}

function canEditTaskProgress(task) {
    return getTaskOwnerEmployeeId(task) === EMPLOYEE_ID;
}

function isTaskActionLocked(taskId) {
    return !!taskId && taskActionLocks.has(String(taskId));
}

function lockTaskAction(taskId) {
    if (!taskId) return;
    taskActionLocks.add(String(taskId));
}

function unlockTaskAction(taskId) {
    if (!taskId) return;
    taskActionLocks.delete(String(taskId));
}

function isTaskOverdue(task) {
    const isCompleted = !!getValue(task, 'isCompleted', 'IsCompleted');
    const expectedEndDate = getTextValue(task, 'expectedEndDate', 'ExpectedEndDate');
    return !isCompleted && !!expectedEndDate && !expectedEndDate.startsWith('0001') && new Date(expectedEndDate) < new Date();
}

function isDeletedTask(task) {
    if (!task) return false;

    const explicitDeleteFlag = !!getValue(task, 'isDeleted', 'IsDeleted');
    const deletedOn = getTextValue(task, 'deletedOn', 'DeletedOn');
    const status = (getTextValue(task, 'status', 'Status') || '').toUpperCase();

    return explicitDeleteFlag || (!!deletedOn && !deletedOn.startsWith('0001')) || status === 'DELETED';
}

function flattenHierarchyMembers() {
    const hierarchyDepartments = hierarchyData && Array.isArray(hierarchyData.departments)
        ? hierarchyData.departments
        : [];

    const seen = new Set();
    const members = [];

    hierarchyDepartments.forEach(function (dept) {
        (dept.subHods || []).forEach(function (subHod) {
            if (subHod && subHod.employeeId && !seen.has(String(subHod.employeeId))) {
                seen.add(String(subHod.employeeId));
                members.push({
                    employeeId: subHod.employeeId,
                    employeeName: subHod.employeeName || '',
                    designation: subHod.designation || '',
                    roleTypeId: 2
                });
            }

            (subHod.executives || []).forEach(function (exec) {
                if (exec && exec.employeeId && !seen.has(String(exec.employeeId))) {
                    seen.add(String(exec.employeeId));
                    members.push({
                        employeeId: exec.employeeId,
                        employeeName: exec.employeeName || '',
                        designation: exec.designation || '',
                        roleTypeId: 3
                    });
                }
            });
        });
    });

    // Fallback: dashboard breakdown already contains the current HOD's scoped team.
    if (members.length === 0 && Array.isArray(breakdownData) && breakdownData.length > 0) {
        breakdownData.forEach(function (member) {
            const employeeId = getValue(member, 'employeeId', 'EmployeeId');
            const roleTypeId = getValue(member, 'roleTypeId', 'RoleTypeId') || 0;
            if (!employeeId || roleTypeId === 1 || seen.has(String(employeeId))) return;

            seen.add(String(employeeId));
            members.push({
                employeeId: employeeId,
                employeeName: getTextValue(member, 'employeeName', 'EmployeeName'),
                designation: getTextValue(member, 'designation', 'Designation'),
                roleTypeId: roleTypeId
            });
        });
    }

    return members;
}

function getKnownTeamMembers() {
    if (ROLE_TYPE_ID === 1 && !IS_ADMIN) {
        return flattenHierarchyMembers().filter(function (member) {
            const roleTypeId = getValue(member, 'roleTypeId', 'RoleTypeId') || 0;
            return roleTypeId === 2 || roleTypeId === 3;
        });
    }
    if (ROLE_TYPE_ID === 2) {
        return (teamMembers || [])
            .filter(function (member) { return getValue(member, 'employeeId', 'EmployeeId') !== EMPLOYEE_ID; })
            .map(function (member) {
                return {
                    employeeId: getValue(member, 'employeeId', 'EmployeeId'),
                    employeeName: getTextValue(member, 'employeeName', 'EmployeeName'),
                    designation: getTextValue(member, 'designation', 'Designation'),
                    roleTypeId: getValue(member, 'roleTypeId', 'RoleTypeId') || 3
                };
            });
    }

    return [];
}

function getKnownTeamMemberIds() {
    return new Set(getKnownTeamMembers().map(function (member) { return String(member.employeeId); }));
}

function filterTasksToKnownTeam(tasks, options) {
    const safeTasks = (Array.isArray(tasks) ? tasks : []).filter(function (task) {
        return !isDeletedTask(task);
    });
    const opts = options || {};
    const knownIds = getKnownTeamMemberIds();

    let filtered = safeTasks;

    if ((ROLE_TYPE_ID === 1 || ROLE_TYPE_ID === 2) && knownIds.size > 0) {
        filtered = filtered.filter(function (task) {
            const assignedId = getValue(task, 'assignedToEmployeeId', 'AssignedToEmployeeId');
            return assignedId && knownIds.has(String(assignedId));
        });
    }

    if (opts.excludeSelf !== false && ROLE_TYPE_ID === 2) {
        filtered = filtered.filter(function (task) {
            return getValue(task, 'assignedToEmployeeId', 'AssignedToEmployeeId') !== EMPLOYEE_ID;
        });
    }

    return filtered;
}

function summarizeTasks(tasks) {
    const summary = { total: 0, pending: 0, inProgress: 0, completed: 0, overdue: 0 };

    (Array.isArray(tasks) ? tasks : []).forEach(function (task) {
        if (isDeletedTask(task)) return;

        summary.total += 1;

        if (!!getValue(task, 'isCompleted', 'IsCompleted')) {
            summary.completed += 1;
            return;
        }

        if (isTaskOverdue(task)) {
            summary.overdue += 1;
            return;
        }

        const status = (getTextValue(task, 'status', 'Status') || '').toUpperCase();
        if (status === 'IN_PROGRESS') {
            summary.inProgress += 1;
        } else {
            summary.pending += 1;
        }
    });

    return summary;
}

function buildBreakdownFromTasks(tasks, members) {
    const map = {};

    (members || []).forEach(function (member) {
        const employeeId = getValue(member, 'employeeId', 'EmployeeId');
        if (!employeeId) return;

        map[String(employeeId)] = {
            employeeId: employeeId,
            employeeName: getTextValue(member, 'employeeName', 'EmployeeName') || 'Unknown',
            roleTypeId: getValue(member, 'roleTypeId', 'RoleTypeId') || 3,
            totalTasks: 0,
            pendingCount: 0,
            inProgressCount: 0,
            completedCount: 0,
            overdueCount: 0
        };
    });

    (Array.isArray(tasks) ? tasks : []).forEach(function (task) {
        if (isDeletedTask(task)) return;

        const employeeId = getValue(task, 'assignedToEmployeeId', 'AssignedToEmployeeId');
        if (!employeeId) return;

        const key = String(employeeId);
        if (!map[key]) {
            map[key] = {
                employeeId: employeeId,
                employeeName: getTextValue(task, 'assignedTo', 'AssignedTo') || 'Unknown',
                roleTypeId: 3,
                totalTasks: 0,
                pendingCount: 0,
                inProgressCount: 0,
                completedCount: 0,
                overdueCount: 0
            };
        }

        map[key].totalTasks += 1;

        if (!!getValue(task, 'isCompleted', 'IsCompleted')) {
            map[key].completedCount += 1;
            return;
        }

        if (isTaskOverdue(task)) {
            map[key].overdueCount += 1;
            return;
        }

        const status = (getTextValue(task, 'status', 'Status') || '').toUpperCase();
        if (status === 'IN_PROGRESS') map[key].inProgressCount += 1;
        else map[key].pendingCount += 1;
    });

    return Object.values(map).sort(function (a, b) {
        return (a.employeeName || '').localeCompare(b.employeeName || '');
    });
}

function updateStatGroup(prefix, summary) {
    const safe = summary || { total: 0, pending: 0, inProgress: 0, completed: 0, overdue: 0 };
    const ids = {
        total: prefix === 'team' ? 'teamStatTotal' : 'statTotal',
        pending: prefix === 'team' ? 'teamStatPending' : 'statPending',
        inProgress: prefix === 'team' ? 'teamStatInProgress' : 'statInProgress',
        completed: prefix === 'team' ? 'teamStatCompleted' : 'statCompleted',
        overdue: prefix === 'team' ? 'teamStatOverdue' : 'statOverdue'
    };

    if (document.getElementById(ids.total)) document.getElementById(ids.total).textContent = safe.total;
    if (document.getElementById(ids.pending)) document.getElementById(ids.pending).textContent = safe.pending;
    if (document.getElementById(ids.inProgress)) document.getElementById(ids.inProgress).textContent = safe.inProgress;
    if (document.getElementById(ids.completed)) document.getElementById(ids.completed).textContent = safe.completed;
    if (document.getElementById(ids.overdue)) document.getElementById(ids.overdue).textContent = safe.overdue;
}

function applyScopedTeamData(tasks) {
    const scopedTasks = filterTasksToKnownTeam(tasks);
    const scopedMembers = getKnownTeamMembers();

    allScopedTeamTasks = scopedTasks;
    employeeProgressMap = buildEmployeeProgressMap(scopedTasks);
    breakdownData = buildBreakdownFromTasks(scopedTasks, scopedMembers);
    updateStatGroup('team', summarizeTasks(scopedTasks));

    if (ROLE_TYPE_ID === 1 && hierarchyData) renderDeptCards();
    if (ROLE_TYPE_ID === 2) renderSubHODTeamPanel();

    return scopedTasks;
}

async function refreshTaskDashboardState() {
    clearDashboardCache();
    allScopedTeamTasks = [];
    lastOwnTasks = [];

    if (ROLE_TYPE_ID === 1 && !hierarchyData) {
        await loadHierarchyTree();
    }

    if (ROLE_TYPE_ID === 1 || ROLE_TYPE_ID === 2 || IS_ADMIN) {
        await loadTeamMembers(true);
    }

    await loadDashboard(true);
    await loadTasks();
}

// ============================================
// INIT
// ============================================
document.addEventListener('DOMContentLoaded', function () {
    console.log('DOM loaded, initializing dashboard...');

    try {
        // Set employee info with visible fallbacks
        var empNameVal = (EMPLOYEE_NAME && EMPLOYEE_NAME.trim()) ? EMPLOYEE_NAME.trim() : 'User';
        var empCodeVal = (EMPLOYEE_CODE && EMPLOYEE_CODE.trim()) ? EMPLOYEE_CODE.trim() : (EMPLOYEE_ID > 0 ? 'EMP-' + EMPLOYEE_ID : '-');

        document.getElementById('employeeName').textContent = empNameVal;

        console.log('Employee data:', { name: empNameVal, code: empCodeVal, empId: EMPLOYEE_ID, roleType: ROLE_TYPE_ID, isAdmin: IS_ADMIN });

        // Banner date badge
        var bannerDateEl = document.getElementById('bannerDate');
        if (bannerDateEl) bannerDateEl.textContent = formatDateFull(new Date());

        const roles = { 1: 'HOD', 2: 'Sub HOD', 3: 'Executive' };
        const badge = document.getElementById('roleBadge');
        if (badge) badge.textContent = IS_ADMIN ? (ADMIN_LABEL || 'Task Admin') : (roles[ROLE_TYPE_ID] || 'User');

        // ---- Role-based features for HOD / Sub HOD ----
        var isManager = (ROLE_TYPE_ID === 1 || ROLE_TYPE_ID === 2 || IS_ADMIN);
        console.log('Is Manager (HOD/SubHOD/Admin):', isManager);

        if (isManager) {
            // Show team stats section
            document.getElementById('teamStatsSection').style.display = 'block';
            // Show task type selector in create modal
            var taskTypeSel = document.getElementById('taskTypeSelector');
            if (taskTypeSel) taskTypeSel.style.display = 'block';
            // Sub-HOD sees own + team tasks
            currentViewMode = 'ALL';

            if (ROLE_TYPE_ID === 1 || ROLE_TYPE_ID === 2 || IS_ADMIN) {
                // HOD / Sub HOD: two-tab layout — My Overview | Team Overview
                if (ROLE_TYPE_ID === 1 && !IS_ADMIN) {
                    document.body.classList.remove('hod-view');
                    document.body.classList.remove('subhod-view');
                } else {
                    document.body.classList.remove('subhod-view');
                    document.body.classList.remove('hod-view');
                }
                subHODCurrentTab = 'my';
                currentViewMode = 'OWN';

                // Inject tab bar after action bar (via JS since Razor view may not be recompiled)
                var actionBar = document.querySelector('.td-action-bar');
                if (actionBar && !document.getElementById('subhodTabs')) {
                    var tabBar = document.createElement('div');
                    tabBar.id = 'subhodTabs';
                    tabBar.style.cssText = 'margin:8px 0 16px;';
                    tabBar.innerHTML = `
                    <div style="display:inline-flex;background:#f1f5f9;border-radius:12px;padding:4px;gap:3px;">
                        <button id="tabMyOverview" onclick="switchSubHODTab('my')" style="padding:9px 24px;border-radius:9px;border:none;font-size:13px;font-weight:600;cursor:pointer;background:linear-gradient(135deg,#1e2d4e,#2d4a7a);color:#fff;box-shadow:0 3px 10px rgba(30,45,78,.35);transition:all .2s;display:flex;align-items:center;gap:7px;">
                            <i class="fas fa-user"></i> My Overview
                        </button>
                        <button id="tabTeamOverview" onclick="switchSubHODTab('team')" style="padding:9px 24px;border-radius:9px;border:1.5px solid #e2e8f0;font-size:13px;font-weight:600;cursor:pointer;background:#fff;color:#1e293b;transition:all .2s;display:flex;align-items:center;gap:7px;">
                            <i class="fas fa-users"></i> Team Overview
                        </button>
                        ${IS_ADMIN ? `<button id="tabAllHodOverview" onclick="switchSubHODTab('allhod')" style="padding:9px 24px;border-radius:9px;border:1.5px solid #e2e8f0;font-size:13px;font-weight:600;cursor:pointer;background:#fff;color:#1e293b;transition:all .2s;display:flex;align-items:center;gap:7px;">
                            <i class="fas fa-user-tie"></i> All HOD Overview
                        </button>` : ''}
                    </div>`;
                    actionBar.parentNode.insertBefore(tabBar, actionBar.nextSibling);
                }

                // MY OVERVIEW (default): show own stats + regular task list
                var myTasksSec = document.getElementById('myTasksSection');
                if (myTasksSec) myTasksSec.style.display = 'block';
                // hodLayout hidden until Team tab activated
                var hodLayout = document.getElementById('hodLayout');
                if (hodLayout) hodLayout.style.display = 'none';
                var teamStat = document.querySelector('#hodLayout #teamStatsSection');
                if (teamStat) teamStat.style.display = 'none';
                var tableSec = document.getElementById('hodTableSection');
                if (tableSec) tableSec.style.display = 'none';

                if (ROLE_TYPE_ID === 1) renderDeptCardsLoading();
            }
        }

        console.log('Init done, loading data...');
    } catch (e) {
        console.error('Init error:', e);
    }

    loadDashboard().catch(e => console.error('Dashboard load error:', e));
    loadTasks().catch(e => console.error('Tasks load error:', e));
    loadTeamMembers().catch(e => console.error('Team load error:', e));
    if (ROLE_TYPE_ID === 1) loadHierarchyTree().catch(e => console.error('Hierarchy error:', e));
});

function formatDateFull(d) {
    const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    return `${d.getDate()}-${months[d.getMonth()]}-${d.getFullYear()}, ${days[d.getDay()]}`;
}

function resetCreateTaskRows() {
    taskRowCount = 1;
    var container = document.getElementById('taskRowsContainer');
    if (!container) return;
    container.innerHTML = createTaskRowHTML(1);
    setTaskRowDefaults();
}

// ============================================
// TASK TYPE SELECTOR (Sub HOD / HOD)
// ============================================
function selectTaskType(type) {
    var previousType = selectedTaskType;
    selectedTaskType = type;

    // Toggle active button
    document.querySelectorAll('.td-type-btn').forEach(function (btn) {
        btn.classList.toggle('active', btn.getAttribute('data-type') === type);
    });

    var assignSection = document.getElementById('assignSection');
    var ctAssignTo = document.getElementById('ctAssignTo');
    var assignSectionLabel = assignSection ? assignSection.querySelector('label') : null;
    var modalSubtitle = document.getElementById('modalSubtitle');
    var submitBtn = document.getElementById('submitTaskBtn');
    var timeSlotSec = document.getElementById('timeSlotSection');
    var autoTimeSlotSec = document.getElementById('autoTimeSlotSection');
    var autoTimeSlotValue = document.getElementById('autoTimeSlotValue');

    if (previousType && previousType !== type) {
        resetCreateTaskRows();
        if (ctAssignTo) ctAssignTo.value = '';
    }

    if (type === 'ASSIGN') {
        // Show assign dropdown
        assignSection.style.display = 'block';
        ctAssignTo.required = true;
        if (timeSlotSec) timeSlotSec.style.display = 'none';
        if (autoTimeSlotSec) autoTimeSlotSec.style.display = 'block';
        if (autoTimeSlotValue) autoTimeSlotValue.textContent = selectedTimeSlot || getCurrentTimeSlot() || '10AM-1PM';
        if (assignSectionLabel) assignSectionLabel.innerHTML = '<i class="fas fa-user-check"></i> ' + (IS_ADMIN ? 'Select HOD' : ((ROLE_TYPE_ID === 1) ? 'Select Team Member' : 'Select Executive')) + ' <span class="td-req">*</span>';
        var assignLabel = IS_ADMIN
            ? 'Assign task to any HOD'
            : ((ROLE_TYPE_ID === 1) ? 'Assign task to Sub HOD or Executive' : 'Assign a task to your team member');
        if (modalSubtitle) modalSubtitle.textContent = assignLabel;
        if (submitBtn) submitBtn.innerHTML = '<i class="fas fa-user-plus"></i> Assign Task';

        // HOD assign dropdown must come from scoped hierarchy members, not generic teamMembers.
        if (ROLE_TYPE_ID === 1 && !IS_ADMIN) {
            var hodMembers = getKnownTeamMembers().filter(function (member) {
                var roleTypeId = getValue(member, 'roleTypeId', 'RoleTypeId') || 0;
                return roleTypeId === 2 || roleTypeId === 3;
            });

            if (hodMembers.length > 0) {
                populateAssignDropdown(hodMembers);
            } else {
                ctAssignTo.innerHTML = '<option value="">No Sub HOD / Executive found</option>';
                if (!hierarchyData) {
                    loadHierarchyTree().then(function () {
                        var freshMembers = getKnownTeamMembers().filter(function (member) {
                            var roleTypeId = getValue(member, 'roleTypeId', 'RoleTypeId') || 0;
                            return roleTypeId === 2 || roleTypeId === 3;
                        });
                        populateAssignDropdown(freshMembers);
                    }).catch(function () {
                        ctAssignTo.innerHTML = '<option value="">Error loading team</option>';
                    });
                }
            }
        } else if (IS_ADMIN) {
            if (allHodMembers.length > 0) {
                populateAssignDropdown(allHodMembers);
            } else {
                ctAssignTo.innerHTML = '<option value="">Loading HODs...</option>';
                loadAllHodMembers(true).then(function () {
                    populateAssignDropdown(allHodMembers);
                }).catch(function () {
                    ctAssignTo.innerHTML = '<option value="">Error loading HODs</option>';
                });
            }
        } else if (teamMembers.length > 0) {
            populateAssignDropdown(teamMembers);
        } else {
            // Fetch from endpoint for Sub-HOD / Director flows
            ctAssignTo.innerHTML = '<option value="">Loading team...</option>';
            fetch('/TaskWeb/GetTeamMembers')
                .then(function (res) { return res.json(); })
                .then(function (r) {
                    if (r.success && r.data && r.data.length > 0) {
                        teamMembers = r.data;
                        populateAssignDropdown(r.data);
                    } else {
                        ctAssignTo.innerHTML = '<option value="">No team members found</option>';
                    }
                })
                .catch(function () {
                    ctAssignTo.innerHTML = '<option value="">Error loading team</option>';
                });
        }
    } else {
        // Hide assign dropdown
        assignSection.style.display = 'none';
        ctAssignTo.required = false;
        ctAssignTo.value = '';
        if (timeSlotSec) timeSlotSec.style.display = 'block';
        if (autoTimeSlotSec) autoTimeSlotSec.style.display = 'none';
        if (modalSubtitle) modalSubtitle.textContent = 'Create a task for yourself';
        if (submitBtn) submitBtn.innerHTML = '<i class="fas fa-paper-plane"></i> Submit Task';
    }
}

// ============================================
// TIME SLOT SELECTOR
// ============================================
function getCurrentTimeSlot() {
    var now = new Date();
    var mins = now.getHours() * 60 + now.getMinutes();
    if (mins >= 10 * 60 && mins < 13 * 60) return '10AM-1PM';
    if (mins >= 13 * 60 && mins < 16 * 60) return '1PM-4PM';
    if (mins >= 16 * 60 && mins < 19 * 60) return '4PM-7PM';
    return null;
}

function selectTimeSlot(slot) {
    selectedTimeSlot = slot;
    document.querySelectorAll('.td-timeslot-btn').forEach(function (btn) {
        btn.classList.toggle('active', btn.getAttribute('data-slot') === slot);
    });
}

function populateAssignDropdown(members) {
    var sel = document.getElementById('ctAssignTo');
    var isDirectorMode = IS_ADMIN;
    var placeholder = isDirectorMode
        ? '-- Select HOD --'
        : ((ROLE_TYPE_ID === 1) ? '-- Select Sub HOD / Executive --' : '-- Select Executive --');
    sel.innerHTML = '<option value="">' + placeholder + '</option>';

    var safeMembers = Array.isArray(members) ? members.slice() : [];

    // Safety guard: HOD must only assign to own Sub-HOD / Executive team, never another HOD.
    if (ROLE_TYPE_ID === 1 && !IS_ADMIN) {
        safeMembers = safeMembers.filter(function (m) {
            var roleTypeId = getValue(m, 'roleTypeId', 'RoleTypeId') || 0;
            return roleTypeId === 2 || roleTypeId === 3;
        });
    }

    safeMembers.forEach(function (m) {
        var employeeId = getValue(m, 'employeeId', 'EmployeeId');
        var name = getTextValue(m, 'employeeName', 'EmployeeName');
        var codeVal = getTextValue(m, 'employeeCode', 'EmployeeCode');
        var roleLabel = getTextValue(m, 'role', 'Role');
        var roleTypeId = getValue(m, 'roleTypeId', 'RoleTypeId') || 0;
        var code = codeVal ? ' (' + codeVal + ')' : '';
        var role = roleLabel
            ? ' - ' + roleLabel
            : (roleTypeId === 2 ? ' - Sub HOD' : (roleTypeId === 3 ? ' - Executive' : ''));
        sel.innerHTML += '<option value="' + employeeId + '">' + esc(name) + code + role + '</option>';
    });

    if (safeMembers.length === 0) {
        sel.innerHTML = '<option value="">' + (isDirectorMode ? '-- Select HOD --' : 'No Sub HOD / Executive found') + '</option>';
    }
}

// ============================================
// DASHBOARD STATS
// ============================================
async function loadDashboard(forceRefresh) {
    try {
        if (forceRefresh) clearDashboardCache();

        dashboardPromise = dashboardPromise || fetch(`${API}/GetDashboard`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ employeeId: EMPLOYEE_ID, roleTypeId: ROLE_TYPE_ID })
        }).then(function (res) { return res.json(); })
            .finally(function () { dashboardPromise = null; });
        const result = await dashboardPromise;
        console.log('GetDashboard raw response:', JSON.stringify(result));
        const isSuccess = result.success || result.Success;
        if (!isSuccess) { console.warn('GetDashboard returned failure:', result); return; }
        const d = result.data || result.Data;

        // Normalize casing (handles both camelCase and PascalCase from API)
        const myTasks = d.myTasks || d.MyTasks;
        const teamTasks = d.teamTasks || d.TeamTasks;
        const teamMemberBreakdown = d.teamMemberBreakdown || d.TeamMemberBreakdown;
        console.log('Dashboard data:', { myTasks, teamTasks, teamMemberBreakdown });

        if (myTasks) {
            document.getElementById('statTotal').textContent = getValue(myTasks, 'totalTasks', 'TotalTasks');
            document.getElementById('statPending').textContent = getValue(myTasks, 'pendingCount', 'PendingCount');
            document.getElementById('statInProgress').textContent = getValue(myTasks, 'inProgressCount', 'InProgressCount');
            document.getElementById('statCompleted').textContent = getValue(myTasks, 'completedCount', 'CompletedCount');
            document.getElementById('statOverdue').textContent = getValue(myTasks, 'overdueCount', 'OverdueCount');
        }
        if (teamTasks) {
            document.getElementById('teamStatTotal').textContent = getValue(teamTasks, 'totalTasks', 'TotalTasks');
            document.getElementById('teamStatPending').textContent = getValue(teamTasks, 'pendingCount', 'PendingCount');
            document.getElementById('teamStatInProgress').textContent = getValue(teamTasks, 'inProgressCount', 'InProgressCount');
            document.getElementById('teamStatCompleted').textContent = getValue(teamTasks, 'completedCount', 'CompletedCount');
            document.getElementById('teamStatOverdue').textContent = getValue(teamTasks, 'overdueCount', 'OverdueCount');
        }
        breakdownData = Array.isArray(teamMemberBreakdown) ? teamMemberBreakdown : [];
        if (breakdownData.length > 0) {
            if (ROLE_TYPE_ID === 2) {
                renderSubHODTeamPanel();
            } else {
                renderTeamGrid(breakdownData);
                // Re-render dept cards with real stats (only if hierarchyData already loaded)
                if (ROLE_TYPE_ID === 1 && hierarchyData) renderDeptCards();
            }
        } else if (ROLE_TYPE_ID === 2) {
            renderSubHODTeamPanel();
        }

        if (allScopedTeamTasks.length && (ROLE_TYPE_ID === 1 || ROLE_TYPE_ID === 2)) {
            updateStatGroup('team', summarizeTasks(allScopedTeamTasks));
        }
        if (lastOwnTasks.length && (ROLE_TYPE_ID === 3 || ((ROLE_TYPE_ID === 1 || ROLE_TYPE_ID === 2) && subHODCurrentTab === 'my'))) {
            updateStatGroup('my', summarizeTasks(lastOwnTasks));
        }
    } catch (err) {
        console.error('Dashboard error:', err);
        dashboardPromise = null;
    }
}

function renderTeamGrid(members) {
    const grid = document.getElementById('teamMemberGrid');
    grid.innerHTML = members.map(m => {
        const employeeId = getValue(m, 'employeeId', 'EmployeeId');
        const role = { 1: 'HOD', 2: 'Sub HOD', 3: 'Executive' }[getValue(m, 'roleTypeId', 'RoleTypeId')] || '';
        const total = getValue(m, 'totalTasks', 'TotalTasks');
        const done = getValue(m, 'completedCount', 'CompletedCount');
        const pct = getEmployeeProgress(employeeId, total, done);
        return `
        <div class="td-team-card" onclick="filterByMember(${employeeId})">
            <div class="td-team-card-header">
                <div class="td-team-avatar"><i class="fas fa-user"></i></div>
                <div><h4>${esc(getTextValue(m, 'employeeName', 'EmployeeName'))}</h4><span class="td-team-role">${role}</span></div>
            </div>
            <div class="td-team-stats">
                <span class="td-mini-stat">${total}</span>
                <span class="td-mini-stat td-c-pending">${getValue(m, 'pendingCount', 'PendingCount')}</span>
                <span class="td-mini-stat td-c-progress">${getValue(m, 'inProgressCount', 'InProgressCount')}</span>
                <span class="td-mini-stat td-c-done">${done}</span>
                <span class="td-mini-stat td-c-overdue">${getValue(m, 'overdueCount', 'OverdueCount')}</span>
            </div>
            <div class="td-progress-bar-mini"><div class="td-progress-fill" style="width:${pct}%"></div></div>
        </div>`;
    }).join('');
}

function filterByMember(empId) {
    currentViewMode = 'TEAM';
    currentTaskFilterOverrides = { assignedToEmployeeId: empId };
    loadTasks();
}

// ============================================
// HOD / SUB-HOD: TAB SWITCHING
// ============================================
function switchSubHODTab(tab) {
    subHODCurrentTab = tab;
    currentPage = 1;
    currentTaskFilterOverrides = {};

    var activeStyle = 'padding:9px 24px;border-radius:9px;border:none;font-size:13px;font-weight:600;cursor:pointer;background:linear-gradient(135deg,#1e2d4e,#2d4a7a);color:#fff;box-shadow:0 3px 10px rgba(30,45,78,.35);transition:all .2s;display:flex;align-items:center;gap:7px;';
    var inactiveStyle = 'padding:9px 24px;border-radius:9px;border:1.5px solid #e2e8f0;font-size:13px;font-weight:600;cursor:pointer;background:#fff;color:#1e293b;transition:all .2s;display:flex;align-items:center;gap:7px;';
    var myBtn = document.getElementById('tabMyOverview');
    var teamBtn = document.getElementById('tabTeamOverview');
    var allHodBtn = document.getElementById('tabAllHodOverview');
    if (myBtn) myBtn.style.cssText = tab === 'my' ? activeStyle : inactiveStyle;
    if (teamBtn) teamBtn.style.cssText = tab === 'team' ? activeStyle : inactiveStyle;
    if (allHodBtn) allHodBtn.style.cssText = tab === 'allhod' ? activeStyle : inactiveStyle;

    if (tab === 'my') {
        currentViewMode = 'OWN';
        document.body.classList.remove('subhod-view');
        document.body.classList.remove('hod-view');
        // Show own stats section
        var myTasksSec = document.getElementById('myTasksSection');
        if (myTasksSec) myTasksSec.style.display = 'block';
        // Hide side-by-side HOD layout
        var hodLayout = document.getElementById('hodLayout');
        if (hodLayout) hodLayout.style.display = 'none';
        // Show regular task list area
        var tlh = document.getElementById('taskListHeader');
        if (tlh) tlh.style.display = 'flex';
        var tlf = document.getElementById('taskListFilters');
        if (tlf) tlf.style.display = 'flex';
        var tl = document.getElementById('taskList');
        if (tl) tl.style.display = 'block';
        var tp = document.getElementById('taskPagination');
        if (tp) tp.style.display = 'flex';
        var hierarchySection = document.getElementById('hierarchySection');
        if (hierarchySection) hierarchySection.style.display = 'none';
        var teamStatsSection = document.querySelector('#hodLayout #teamStatsSection');
        if (teamStatsSection) teamStatsSection.style.display = 'none';
        var hodTableSection = document.getElementById('hodTableSection');
        if (hodTableSection) hodTableSection.style.display = 'none';
    } else {
        currentViewMode = 'TEAM';
        if (ROLE_TYPE_ID === 1 && !IS_ADMIN) document.body.classList.add('hod-view');
        if (ROLE_TYPE_ID === 2 || IS_ADMIN) document.body.classList.add('subhod-view');
        // Hide own stats
        var myTasksSec = document.getElementById('myTasksSection');
        if (myTasksSec) myTasksSec.style.display = 'none';
        // Show side-by-side layout
        var hodLayout = document.getElementById('hodLayout');
        if (hodLayout) hodLayout.style.display = 'flex';
        var teamStatsSection = document.querySelector('#hodLayout #teamStatsSection');
        if (teamStatsSection) teamStatsSection.style.display = 'block';
        var hodTableSection = document.getElementById('hodTableSection');
        if (hodTableSection) hodTableSection.style.display = 'block';
        // Hide regular task list (it lives outside hodLayout)
        ['taskListHeader', 'taskListFilters', 'taskList', 'taskPagination'].forEach(function (id) {
            var el = document.getElementById(id);
            if (el) el.style.display = 'none';
        });
        // Render right panel
        if (tab === 'allhod' && IS_ADMIN) {
            loadAllHodMembers().then(function () {
                renderAdminHodPanel();
            });
        } else if (ROLE_TYPE_ID === 1) {
            if (hierarchyData) renderDeptCards();
            else {
                renderDeptCardsLoading();
                loadHierarchyTree();
            }
        } else {
            if (breakdownData.length) {
                renderSubHODTeamPanel();
            } else {
                renderSubHODTeamLoading();
                loadDashboard();
            }
        }
    }
    loadTasks();
}

// ============================================
// LOAD TASKS
// ============================================
async function loadTasks(extra = {}) {
    showLoader();
    try {
        if (ROLE_TYPE_ID === 1 && currentViewMode === 'TEAM' && !hierarchyData) {
            await loadHierarchyTree();
        }
        if ((ROLE_TYPE_ID === 1 || ROLE_TYPE_ID === 2 || IS_ADMIN) && teamMembers.length === 0) {
            await loadTeamMembers(true);
        }
        if (IS_ADMIN && subHODCurrentTab === 'allhod' && allHodMembers.length === 0) {
            await loadAllHodMembers(true);
        }

        if (extra && Object.keys(extra).length > 0) {
            currentTaskFilterOverrides = Object.assign({}, currentTaskFilterOverrides, extra);
        }

        const isAllHodView = IS_ADMIN && subHODCurrentTab === 'allhod';
        const pageLimit = currentViewMode === 'TEAM' || isAllHodView ? 1000 : 500;
        const filter = {
            page: currentPage, limit: pageLimit,
            status: document.getElementById('filterStatus').value || null,
            priority: document.getElementById('filterPriority').value || null,
            taskType: document.getElementById('filterTaskType').value || null,
            employeeId: EMPLOYEE_ID, roleTypeId: ROLE_TYPE_ID,
            viewMode: isAllHodView ? 'ALL' : currentViewMode, sortBy: 'created_on', sortOrder: 'DESC',
            ...((ROLE_TYPE_ID === 3 || ((ROLE_TYPE_ID === 1 || ROLE_TYPE_ID === 2) && subHODCurrentTab === 'my')) ? { assignedToEmployeeId: EMPLOYEE_ID } : {}),
            ...currentTaskFilterOverrides
        };
        // HOD in TEAM view: respect user's filter selection (do not force ASSIGNED)
        // This allows HOD to see all task types (SELF and ASSIGNED) from their team
        const res = await fetch(`${API}/GetAllTasks`, {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(filter)
        });
        const result = await res.json();
        console.log('GetAllTasks response:', { success: result.success || result.Success, count: (result.data || result.Data || []).length });
        const isOk = result.success || result.Success;
        let tasks = result.data || result.Data || [];
        if (!Array.isArray(tasks)) tasks = [];
        tasks = tasks.filter(function (task) { return !isDeletedTask(task); });
        const totalPages = tasks.length > 0 ? (tasks[0].totalPages || tasks[0].TotalPages || 1) : 1;
        const totalCount = tasks.length > 0 ? (tasks[0].totalCount || tasks[0].TotalCount || 0) : 0;
        if (isOk) {
            if (isAllHodView) {
                const allHodIds = new Set((allHodMembers || []).map(function (member) {
                    return String(getValue(member, 'employeeId', 'EmployeeId'));
                }));
                allHodTasks = tasks.filter(function (task) {
                    var ownerId = getValue(task, 'assignedToEmployeeId', 'AssignedToEmployeeId');
                    return ownerId && allHodIds.has(String(ownerId));
                });
                updateStatGroup('team', summarizeTasks(allHodTasks));
                renderAdminHodPanel();
                renderHODTable(allHodTasks);
            } else if (ROLE_TYPE_ID === 1) {
                if (subHODCurrentTab === 'team') {
                    allTeamTasks = filterTasksToKnownTeam(tasks, { excludeSelf: false });
                    applyScopedTeamData(allTeamTasks);
                    renderHODTable(allTeamTasks);
                } else {
                    lastOwnTasks = tasks.slice();
                    updateStatGroup('my', summarizeTasks(tasks));
                    renderTaskList(tasks);
                    renderPagination(currentPage, totalPages, totalCount);
                }
            } else if (ROLE_TYPE_ID === 2) {
                if (subHODCurrentTab === 'team') {
                    // Team tasks — build progress map from actual percent_complete
                    const scopedTeamTasks = applyScopedTeamData(tasks);
                    lastOwnTasks = [];
                    renderSubHODTaskList(scopedTeamTasks);
                } else {
                    // My Overview — own tasks only; fetch team tasks separately for progress map
                    lastOwnTasks = tasks.slice();
                    updateStatGroup('my', summarizeTasks(tasks));
                    renderTaskList(tasks);
                    renderPagination(currentPage, totalPages, totalCount);
                    loadSubHODTeamProgressMap();
                }
            } else {
                lastOwnTasks = tasks.slice();
                updateStatGroup('my', summarizeTasks(tasks));
                renderTaskList(tasks);
                renderPagination(currentPage, totalPages, totalCount);
            }
        }
    } catch (err) { console.error(err); }
    finally { hideLoader(); }
}

// Fetch team tasks in background solely to build employeeProgressMap for the right panel
async function loadSubHODTeamProgressMap() {
    try {
        const res = await fetch(`${API}/GetAllTasks`, {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                page: 1, limit: 1000,
                employeeId: EMPLOYEE_ID, roleTypeId: ROLE_TYPE_ID,
                viewMode: 'TEAM', sortBy: 'created_on', sortOrder: 'DESC'
            })
        });
        const result = await res.json();
        const tasks = result.data || result.Data || [];
        if (Array.isArray(tasks)) applyScopedTeamData(tasks.filter(function (task) { return !isDeletedTask(task); }));
    } catch (e) { console.error('Team progress map error:', e); }
}

function renderTaskList(tasks) {
    indexTasksForDetails(tasks);
    const c = document.getElementById('taskList');
    if (!tasks.length) {
        c.innerHTML = '<div class="td-empty"><i class="fas fa-inbox"></i><h3>No Tasks Found</h3><p>Create a new task to get started</p></div>';
        return;
    }
    c.innerHTML = tasks.map(function (t, index) {
        const taskId = getTextValue(t, 'taskId', 'TaskId');
        const taskName = getTextValue(t, 'taskName', 'TaskName');
        const description = getTextValue(t, 'description', 'Description');
        const assignedTo = getTextValue(t, 'assignedTo', 'AssignedTo') || 'Unassigned';
        const assignedByName = getTextValue(t, 'assignedByName', 'AssignedByName');
        const startDate = getTextValue(t, 'startDate', 'StartDate');
        const expectedEndDate = getTextValue(t, 'expectedEndDate', 'ExpectedEndDate');
        const completionDate = getTextValue(t, 'completionDate', 'CompletionDate');
        const priority = getTextValue(t, 'priority', 'Priority') || 'MEDIUM';
        const taskType = getTextValue(t, 'taskType', 'TaskType');
        const status = getTextValue(t, 'status', 'Status') || 'PENDING';
        const isCompleted = !!getValue(t, 'isCompleted', 'IsCompleted');
        const pct = getValue(t, 'percentComplete', 'PercentComplete');
        const overdue = !isCompleted && new Date(expectedEndDate) < new Date();
        const sc = isCompleted ? 'completed' : (overdue ? 'overdue' : status.toLowerCase());
        const canReassign = (ROLE_TYPE_ID === 1 || ROLE_TYPE_ID === 2) && !isCompleted;
        const slot = getTextValue(t, 'slot', 'Slot');
        const parsedTaskDetails = parseSlot(description);
        const descText = parsedTaskDetails.descText;
        const displaySlot = slot || parsedTaskDetails.slot;
        const canProgress = canEditTaskProgress(t);
        return `
        <div class="td-task-card" data-task-id="${taskId}">
            <div class="td-task-top">
                <div class="td-task-left">
                    <span class="td-priority" style="background:#e2e8f0;color:#334155;">${index + 1}</span>
                    <span class="td-priority td-priority-${priority.toLowerCase()}">${priority}</span>
                    <span class="td-task-type ${taskType === 'ASSIGNED' ? 'td-type-assigned' : 'td-type-self'}">${taskType === 'ASSIGNED' ? 'Assigned' : 'Self'}</span>
                    <span class="td-status td-status-${sc}">${isCompleted ? 'Completed' : (overdue ? 'Overdue' : fmtStatus(status))}</span>
                    ${displaySlot ? `<span class="td-timeslot-badge"><i class="fas fa-clock"></i> ${esc(displaySlot)}</span>` : ''}
                </div>
                <div class="td-task-right"><span class="td-pct">${pct}%</span></div>
            </div>
            <h3 class="td-task-title">${esc(taskName)}</h3>
            ${descText ? `<p class="td-task-desc">${esc(descText)}</p>` : ''}
            <div class="td-task-meta">
                <span><i class="fas fa-user-circle"></i> ${esc(assignedTo)}</span>
                ${assignedByName ? `<span><i class="fas fa-user-tag"></i> By: ${esc(assignedByName)}</span>` : ''}
            </div>
            <div class="td-progress-bar"><div class="td-progress-fill ${pct >= 100 ? 'td-pf-done' : ''}" style="width:${pct}%"></div></div>
            <div class="td-task-dates">
                <span><i class="fas fa-calendar-plus"></i> ${fmtDate(startDate)}</span>
                <span class="${overdue ? 'td-text-danger' : ''}"><i class="fas fa-calendar-check"></i> ${fmtDate(expectedEndDate)}</span>
                ${completionDate ? `<span><i class="fas fa-check"></i> ${fmtDate(completionDate)}</span>` : ''}
            </div>
            <div class="td-task-actions">${buildTaskActions(taskId, pct, canReassign, isCompleted, canProgress)}</div>
        </div>`;
    }).join('');
}

// ============================================
// PAGINATION
// ============================================
function renderPagination(page, totalPages, totalCount) {
    const el = document.getElementById('taskPagination');
    if (!el) return;
    if (totalPages <= 1) { el.style.display = 'none'; el.innerHTML = ''; return; }
    el.style.display = 'flex';
    let html = '';
    html += `<button class="td-page-btn" onclick="goToPage(${page - 1})" ${page <= 1 ? 'disabled' : ''}><i class="fas fa-chevron-left"></i></button>`;
    for (let i = 1; i <= totalPages; i++) {
        if (totalPages > 7 && i > 2 && i < totalPages - 1 && Math.abs(i - page) > 1) {
            if (i === 3 || i === totalPages - 2) html += `<span class="td-page-dots">…</span>`;
            continue;
        }
        html += `<button class="td-page-btn ${i === page ? 'active' : ''}" onclick="goToPage(${i})">${i}</button>`;
    }
    html += `<button class="td-page-btn" onclick="goToPage(${page + 1})" ${page >= totalPages ? 'disabled' : ''}><i class="fas fa-chevron-right"></i></button>`;
    html += `<span class="td-page-info">Page ${page} of ${totalPages} &nbsp;(${totalCount} tasks)</span>`;
    el.innerHTML = html;
}

function goToPage(page) {
    if (page < 1) return;
    currentPage = page;
    loadTasks();
    document.getElementById('taskList').scrollIntoView({ behavior: 'smooth', block: 'start' });
}

// ============================================
// VIEW MODE
// ============================================
function switchViewMode(mode) {
    currentViewMode = mode;
    currentPage = 1;
    currentTaskFilterOverrides = {};
    document.getElementById('filterTaskType').value = '';
    loadTasks();
}
function updateToggle(mode) { document.querySelectorAll('.td-toggle-btn').forEach(b => b.classList.toggle('active', b.dataset.mode === mode)); }

// ============================================
// CREATE TASK MODAL
// ============================================
function openCreateTaskModal() {
    console.log('openCreateTaskModal called, role:', ROLE_TYPE_ID, 'teamMembers:', teamMembers.length);

    document.getElementById('createTaskForm').reset();
    taskRowCount = 1;
    document.getElementById('taskRowsContainer').innerHTML = createTaskRowHTML(1);

    const today = new Date().toISOString().split('T')[0];
    setTaskRowDefaults();

    // Set modal employee info
    var empNameVal = (EMPLOYEE_NAME && EMPLOYEE_NAME.trim()) ? EMPLOYEE_NAME.trim() : 'User';
    var empCodeVal = (EMPLOYEE_CODE && EMPLOYEE_CODE.trim()) ? EMPLOYEE_CODE.trim() : (EMPLOYEE_ID > 0 ? 'EMP-' + EMPLOYEE_ID : '-');
    document.getElementById('modalEmpCode').textContent = empCodeVal;
    document.getElementById('modalEmpName').textContent = empNameVal;
    document.getElementById('modalDate').textContent = formatDateFull(new Date());

    var isHOD = (ROLE_TYPE_ID === 1);
    var isSubHOD = (ROLE_TYPE_ID === 2);
    var isManager = (isHOD || isSubHOD || IS_ADMIN);
    var taskTypeSel = document.getElementById('taskTypeSelector');
    var timeSlotSec = document.getElementById('timeSlotSection');
    var autoTimeSlotSec = document.getElementById('autoTimeSlotSection');
    var autoTimeSlotValue = document.getElementById('autoTimeSlotValue');

    // Reset time slot, then auto-select based on current time
    selectedTimeSlot = null;
    document.querySelectorAll('.td-timeslot-btn').forEach(function (b) { b.classList.remove('active'); });
    var autoSlot = getCurrentTimeSlot() || '10AM-1PM';

    if (isHOD) {
        // HOD: allow self task and assign task
        if (taskTypeSel) taskTypeSel.style.display = 'block';
        if (timeSlotSec) timeSlotSec.style.display = 'block';
        if (autoTimeSlotSec) autoTimeSlotSec.style.display = 'none';
        selectedTaskType = 'SELF';
        selectTaskType('SELF');
    } else if (isSubHOD || IS_ADMIN) {
        // Sub HOD / Director: show toggle and time slot for both self and assign
        if (taskTypeSel) taskTypeSel.style.display = 'block';
        if (timeSlotSec) timeSlotSec.style.display = 'block';
        if (autoTimeSlotSec) autoTimeSlotSec.style.display = 'none';
        selectedTaskType = IS_ADMIN ? 'ASSIGN' : 'SELF';
        selectTaskType(selectedTaskType);
    } else {
        // Executive: always show time slot
        if (taskTypeSel) taskTypeSel.style.display = 'none';
        if (timeSlotSec) timeSlotSec.style.display = 'block';
        if (autoTimeSlotSec) autoTimeSlotSec.style.display = 'none';
        selectedTaskType = 'SELF';
        selectTaskType('SELF');
    }

    // Auto-select the current time slot
    if (autoSlot) selectTimeSlot(autoSlot);
    if (autoTimeSlotValue) autoTimeSlotValue.textContent = autoSlot;

    // Load team members if not loaded yet
    if (isManager && teamMembers.length === 0) {
        loadTeamMembers().then(function () {
            console.log('Team members loaded in modal:', teamMembers.length);
            if (selectedTaskType === 'ASSIGN') selectTaskType('ASSIGN');
        });
    }
    if (IS_ADMIN && allHodMembers.length === 0) {
        loadAllHodMembers().then(function () {
            if (selectedTaskType === 'ASSIGN') selectTaskType('ASSIGN');
        });
    }

    document.getElementById('createTaskModal').style.display = 'flex';
}

function closeCreateTaskModal() {
    document.getElementById('createTaskModal').style.display = 'none';
}

function createTaskRowHTML(num) {
    return `
    <div class="td-task-row" data-index="${num}">
        <span class="td-task-chip">${num}</span>
        <div class="td-task-row-main">
            <div class="td-task-field">
                <span class="td-task-field-label">Task</span>
                <input type="text" class="td-task-input" placeholder="Describe task..." required />
            </div>
            <div class="td-task-field">
                <span class="td-task-field-label">Start Date</span>
                <input type="date" class="td-task-start-date" required />
            </div>
            <div class="td-task-field">
                <span class="td-task-field-label">End Date</span>
                <input type="date" class="td-task-deadline" required />
            </div>
            <div class="td-task-field">
                <span class="td-task-field-label">Priority</span>
                <select class="td-task-priority">
                    <option value="LOW">Low</option>
                    <option value="MEDIUM" selected>Medium</option>
                    <option value="HIGH">High</option>
                    <option value="CRITICAL">Critical</option>
                </select>
            </div>
        </div>
        <textarea class="td-task-remarks" rows="2" placeholder="Remarks for this task"></textarea>
        ${num > 1 ? '<button type="button" class="td-task-remove" onclick="removeTaskRow(this)"><i class="fas fa-times"></i></button>' : ''}
    </div>`;
}

function setTaskRowDefaults() {
    var defaultStartDate = new Date().toISOString().split('T')[0];
    var defaultDeadline = new Date(Date.now() + 7 * 86400000).toISOString().split('T')[0];
    document.querySelectorAll('.td-task-row').forEach(function (row) {
        var startDateInput = row.querySelector('.td-task-start-date');
        var deadlineInput = row.querySelector('.td-task-deadline');
        if (startDateInput && !startDateInput.value) startDateInput.value = defaultStartDate;
        if (deadlineInput && !deadlineInput.value) deadlineInput.value = defaultDeadline;
    });
}

function addTaskRow() {
    taskRowCount++;
    document.getElementById('taskRowsContainer').insertAdjacentHTML('beforeend', createTaskRowHTML(taskRowCount));
    setTaskRowDefaults();
}

function removeTaskRow(btn) {
    btn.closest('.td-task-row').remove();
    document.querySelectorAll('.td-task-row').forEach((row, i) => {
        row.querySelector('.td-task-chip').textContent = i + 1;
    });
}

async function submitCreateTask(e) {
    e.preventDefault();
    if (createTaskInFlight) return;

    // Validate: if ASSIGN mode, must select an executive
    if (selectedTaskType === 'ASSIGN') {
        var assignToVal = document.getElementById('ctAssignTo').value;
        if (!assignToVal) {
            showNotification('Please select a person to assign', 'error');
            return;
        }
    }

    // Validate: time slot required for HOD, Sub HOD, and Executive
    var needsSlot = (ROLE_TYPE_ID === 1 || ROLE_TYPE_ID === 2 || ROLE_TYPE_ID === 3);
    if (needsSlot && !selectedTimeSlot) {
        showNotification('Please select a time slot', 'error');
        return;
    }

    createTaskInFlight = true;
    var submitButton = document.getElementById('submitTaskBtn');
    if (submitButton) submitButton.disabled = true;
    showLoader();

    const rows = document.querySelectorAll('.td-task-row');
    const assignTo = (selectedTaskType === 'ASSIGN') ? document.getElementById('ctAssignTo').value : '';
    let successCount = 0;
    let errorCount = 0;

    for (const row of rows) {
        const taskName = row.querySelector('.td-task-input').value.trim();
        if (!taskName) continue;

        const startDate = row.querySelector('.td-task-start-date').value;
        const deadline = row.querySelector('.td-task-deadline').value;
        const priority = row.querySelector('.td-task-priority').value;
        const remarks = (row.querySelector('.td-task-remarks').value || '').trim();

        if (!startDate || !deadline) {
            errorCount++;
            continue;
        }

        const body = {
            taskName,
            description: remarks,
            projectName: '',
            moduleName: '',
            assignedToEmployeeId: assignTo ? parseInt(assignTo) : EMPLOYEE_ID,
            createdByEmployeeId: EMPLOYEE_ID,
            startDate, expectedEndDate: deadline, priority,
            slot: selectedTimeSlot
        };

        try {
            const res = await fetch(`${API}/CreateTask`, {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body)
            });
            const result = await res.json();
            console.log('CreateTask response:', result);
            if (result.success) successCount++; else {
                console.error('CreateTask failed:', result);
                errorCount++;
            }
        } catch (err) { console.error('CreateTask exception:', err); errorCount++; }
    }

    hideLoader();
    createTaskInFlight = false;
    if (submitButton) submitButton.disabled = false;

    if (successCount > 0) {
        var msg = selectedTaskType === 'ASSIGN'
            ? successCount + ' task(s) assigned successfully!'
            : successCount + ' task(s) created!';
        showNotification(msg, 'success');
        closeCreateTaskModal();
        await refreshTaskDashboardState();
    }
    if (errorCount > 0) showNotification(errorCount + ' task(s) failed', 'error');
}

// ============================================
// COMPLETE / DELETE
// ============================================
async function completeTask(taskId) {
    if (!confirm('Mark as completed?')) return;
    if (isTaskActionLocked(taskId)) return;
    lockTaskAction(taskId);
    showLoader();
    try {
        const lastModifiedByEmployeeId = Number.isFinite(Number(EMPLOYEE_ID)) && Number(EMPLOYEE_ID) > 0
            ? Number(EMPLOYEE_ID)
            : null;
        const lastModifiedBy = (typeof EMPLOYEE_NAME === 'string' && EMPLOYEE_NAME.trim())
            ? EMPLOYEE_NAME.trim()
            : null;
        const res = await fetch(`${API}/CompleteTask`, {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ taskId, lastModifiedBy, lastModifiedByEmployeeId })
        });
        const r = await res.json().catch(() => null);
        if (!res.ok) {
            showNotification(r?.message || r?.Message || `Failed (${res.status})`, 'error');
            return;
        }
        if (r.success) { showNotification('Task completed!', 'success'); await refreshTaskDashboardState(); }
        else showNotification(r.message || 'Failed', 'error');
    } catch (e) { showNotification('Error', 'error'); }
    finally { hideLoader(); unlockTaskAction(taskId); }
}

async function deleteTask(taskId) {
    if (!confirm('Delete this task?')) return;
    if (isTaskActionLocked(taskId)) return;
    const deletedReasonInput = prompt('Please enter delete reason:');
    if (deletedReasonInput === null) return;

    const deletedReason = deletedReasonInput.trim();
    if (!deletedReason) {
        showNotification('Delete reason is required', 'error');
        return;
    }

    lockTaskAction(taskId);
    showLoader();
    try {
        const deletedByEmployeeId = Number.isFinite(Number(EMPLOYEE_ID)) && Number(EMPLOYEE_ID) > 0
            ? Number(EMPLOYEE_ID)
            : null;
        const deletedByName = (typeof EMPLOYEE_NAME === 'string' && EMPLOYEE_NAME.trim())
            ? EMPLOYEE_NAME.trim()
            : null;

        const res = await fetch(`${API}/DeleteTask`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                taskId,
                deletedByEmployeeId,
                deletedByName,
                deletedAt: new Date().toISOString(),
                deletedReason
            })
        });
        const r = await res.json();
        if (r.success) { showNotification('Deleted', 'success'); await refreshTaskDashboardState(); }
        else showNotification(r.message || 'Failed', 'error');
    } catch (e) { showNotification('Error', 'error'); }
    finally { hideLoader(); unlockTaskAction(taskId); }
}

// ============================================
// PROGRESS
// ============================================
async function openProgressModal(taskId, pct) {
    document.getElementById('progressTaskId').value = taskId;
    document.getElementById('progressPercent').value = pct;
    document.getElementById('progressPercentLabel').textContent = pct + '%';
    document.getElementById('progressText').value = '';
    try {
        const res = await fetch(`${API}/GetProgressUpdates/${taskId}`);
        const r = await res.json();
        const h = document.getElementById('progressHistory');
        if (r.success && r.data && r.data.length) {
            h.innerHTML = '<h4>Previous Updates</h4>' + r.data.map(u => `
                <div class="td-progress-item">
                    <div class="td-progress-item-header">
                        <strong>${esc(u.employeeName)}</strong><span>${fmtDate(u.updateDate)}</span>
                        <span class="td-pct-small">${u.percentComplete}%</span>
                    </div>
                    <p>${esc(u.updateText)}</p>
                </div>`).join('');
        } else h.innerHTML = '<p class="td-muted">No updates yet</p>';
    } catch (e) { console.error(e); }
    document.getElementById('progressModal').style.display = 'flex';
}
function closeProgressModal() { document.getElementById('progressModal').style.display = 'none'; }

async function submitProgressUpdate() {
    if (progressUpdateInFlight) return;
    const taskId = document.getElementById('progressTaskId').value;
    if (isTaskActionLocked(taskId)) return;
    const text = document.getElementById('progressText').value.trim();
    const pct = parseInt(document.getElementById('progressPercent').value);
    // note is optional
    progressUpdateInFlight = true;
    lockTaskAction(taskId);
    showLoader();
    try {
        const res = await fetch(`${API}/AddProgressUpdate`, {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ taskId, employeeId: EMPLOYEE_ID, updateText: text, percentComplete: pct })
        });
        const r = await res.json();
        if (r.success) {
            // Optimistically update progress bar in card without waiting for reload
            const updatedTaskId = document.getElementById('progressTaskId').value;
            const updatedPct = parseInt(document.getElementById('progressPercent').value);
            const card = document.querySelector(`[data-task-id="${updatedTaskId}"]`);
            if (card) {
                const fill = card.querySelector('.td-progress-fill');
                const pctEl = card.querySelector('.td-pct');
                if (fill) { fill.style.width = updatedPct + '%'; if (updatedPct >= 100) fill.classList.add('td-pf-done'); }
                if (pctEl) pctEl.textContent = updatedPct + '%';
            }
            showNotification('Updated!', 'success');
            closeProgressModal();
            await refreshTaskDashboardState();
        }
        else showNotification(r.message || 'Failed', 'error');
    } catch (e) { showNotification('Error', 'error'); }
    finally {
        hideLoader();
        progressUpdateInFlight = false;
        unlockTaskAction(taskId);
    }
}

// ============================================
// REASSIGN
// ============================================
async function openReassignModal(taskId) {
    document.getElementById('reassignTaskId').value = taskId;
    document.getElementById('reassignNotes').value = '';
    const sel = document.getElementById('reassignTo');
    sel.innerHTML = '';
    teamMembers.forEach(m => sel.innerHTML += `<option value="${m.employeeId}">${esc(m.employeeName)}</option>`);
    document.getElementById('reassignModal').style.display = 'flex';
}
function closeReassignModal() { document.getElementById('reassignModal').style.display = 'none'; }

async function submitReassign() {
    if (reassignInFlight) return;
    const taskId = document.getElementById('reassignTaskId').value;
    if (isTaskActionLocked(taskId)) return;
    const toId = document.getElementById('reassignTo').value;
    const notes = document.getElementById('reassignNotes').value;
    if (!toId) { showNotification('Select employee', 'error'); return; }
    reassignInFlight = true;
    lockTaskAction(taskId);
    showLoader();
    try {
        const res = await fetch(`${API}/ReassignTask`, {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ taskId, newAssignedToEmployeeId: parseInt(toId), reassignedByEmployeeId: EMPLOYEE_ID, notes })
        });
        const r = await res.json();
        if (r.success) { showNotification('Reassigned!', 'success'); closeReassignModal(); await refreshTaskDashboardState(); }
        else showNotification(r.message || 'Failed', 'error');
    } catch (e) { showNotification('Error', 'error'); }
    finally {
        hideLoader();
        reassignInFlight = false;
        unlockTaskAction(taskId);
    }
}

// ============================================
// TEAM MEMBERS
// ============================================
async function loadTeamMembers(forceRefresh) {
    if (ROLE_TYPE_ID !== 1 && ROLE_TYPE_ID !== 2 && !IS_ADMIN) return;
    try {
        if (forceRefresh) teamMembers = [];

        const res = await fetch('/TaskWeb/GetTeamMembers');
        const r = await res.json();
        const members = r.data || r.Data || [];
        if ((r.success || r.Success) && Array.isArray(members)) {
            teamMembers = members;
            console.log('Team members loaded:', teamMembers.length, teamMembers.map(m => getTextValue(m, 'employeeName', 'EmployeeName')));
            if (ROLE_TYPE_ID === 2 && allScopedTeamTasks.length) applyScopedTeamData(allScopedTeamTasks);
        }
    } catch (e) { console.error('loadTeamMembers error:', e); }
}

async function loadAllHodMembers(forceRefresh) {
    if (!IS_ADMIN) return;
    try {
        if (forceRefresh) allHodMembers = [];

        const res = await fetch('/TaskWeb/GetTeamMembers?scope=allhods');
        const r = await res.json();
        const members = r.data || r.Data || [];
        if ((r.success || r.Success) && Array.isArray(members)) {
            allHodMembers = members;
        }
    } catch (e) {
        console.error('loadAllHodMembers error:', e);
    }
}

// ============================================
// HOD DEPT CARDS
// ============================================
async function loadHierarchyTree() {
    try {
        // Use server-preloaded data if available (no extra API call needed)
        if (typeof HIERARCHY_PRELOAD !== 'undefined' && HIERARCHY_PRELOAD && HIERARCHY_PRELOAD.success) {
            hierarchyData = HIERARCHY_PRELOAD;
            if (ROLE_TYPE_ID === 1 && allTeamTasks.length) applyScopedTeamData(allTeamTasks);
            renderDeptCards();
            return;
        }
        renderDeptCardsLoading();
        const res = await fetch('/TaskWeb/GetHierarchyTree');
        const r = await res.json();
        if (r.success) {
            hierarchyData = r;
            if (ROLE_TYPE_ID === 1 && allTeamTasks.length) applyScopedTeamData(allTeamTasks);
            renderDeptCards();
        }
    } catch (e) { console.error('Hierarchy tree error:', e); }
}

function renderDeptCardsLoading() {
    const section = document.getElementById('hierarchySection');
    if (!section || ROLE_TYPE_ID !== 1) return;
    if (subHODCurrentTab !== 'team') return;
    if (hierarchyData) return;
    section.style.display = 'block';

    let cards = '';
    for (let i = 0; i < 4; i++) {
        cards += `
        <div class="td-dept-card loading">
            <div class="td-dept-header">
                <div class="td-dept-skeleton td-dept-skeleton-icon"></div>
                <div style="flex:1">
                    <div class="td-dept-skeleton td-dept-skeleton-title"></div>
                    <div class="td-dept-skeleton td-dept-skeleton-sub"></div>
                </div>
            </div>
            <div class="td-dept-stats">
                <div class="td-dept-stat"><div class="td-dept-skeleton td-dept-skeleton-stat"></div></div>
                <div class="td-dept-stat"><div class="td-dept-skeleton td-dept-skeleton-stat"></div></div>
                <div class="td-dept-stat"><div class="td-dept-skeleton td-dept-skeleton-stat"></div></div>
                <div class="td-dept-stat"><div class="td-dept-skeleton td-dept-skeleton-stat"></div></div>
                <div class="td-dept-stat"><div class="td-dept-skeleton td-dept-skeleton-stat"></div></div>
            </div>
            <div class="td-dept-prog-section">
                <div class="td-dept-prog-head">
                    <div class="td-dept-skeleton td-dept-skeleton-sub" style="width:130px"></div>
                    <div class="td-dept-skeleton td-dept-skeleton-pct"></div>
                </div>
                <div class="td-dept-skeleton td-dept-skeleton-progress"></div>
            </div>
        </div>`;
    }

    section.innerHTML = '<div class="td-section-header"><h2><i class="fas fa-building"></i> Departments</h2></div><div class="td-dept-grid" style="flex-direction:column">' + cards + '</div>';
}

function getEmpStats(empId) {
    return breakdownData.find(function (b) { return String(getValue(b, 'employeeId', 'EmployeeId')) === String(empId); }) || {};
}

function renderDeptCards() {
    if (!hierarchyData) return;
    const section = document.getElementById('hierarchySection');
    if (!section) return;
    if (subHODCurrentTab !== 'team') {
        section.style.display = 'none';
        return;
    }
    section.style.display = 'block';

    const depts = hierarchyData.departments || [];
    const bgColors = ['#4f46e5', '#0891b2', '#059669', '#d97706', '#dc2626', '#7c3aed', '#0284c7', '#16a34a'];

    let html = '<div class="td-section-header"><h2><i class="fas fa-building"></i> Departments</h2></div><div class="td-dept-grid">';

    depts.forEach(function (dept, di) {
        const bg = bgColors[di % bgColors.length];
        // Collect all employees in this dept
        const allMembers = [];
        (dept.subHods || []).forEach(function (sh) {
            allMembers.push({ employeeId: sh.employeeId, employeeName: sh.employeeName, designation: sh.designation, role: 'Sub HOD' });
            (sh.executives || []).forEach(function (e) {
                allMembers.push({ employeeId: e.employeeId, employeeName: e.employeeName, designation: e.designation, role: 'Executive' });
            });
        });
        // Sum task stats from breakdownData
        let total = 0, pending = 0, inProg = 0, completed = 0, overdue = 0;
        allMembers.forEach(function (m) {
            const s = getEmpStats(m.employeeId);
            total += getValue(s, 'totalTasks', 'TotalTasks');
            pending += getValue(s, 'pendingCount', 'PendingCount');
            inProg += getValue(s, 'inProgressCount', 'InProgressCount');
            completed += getValue(s, 'completedCount', 'CompletedCount');
            overdue += getValue(s, 'overdueCount', 'OverdueCount');
        });
        const pct = getDepartmentProgress(allMembers, total, completed);

        // Encode members list for click handler
        const membersJson = JSON.stringify(allMembers).replace(/'/g, '&#39;');

        html += `
        <div class="td-dept-card" onclick="toggleDeptEmployees(this)">
            <div class="td-dept-header">
                <div class="td-dept-icon" style="background:${bg}"><i class="fas fa-building"></i></div>
                <div>
                    <div class="td-dept-title">${esc(dept.departmentName)}</div>
                    <div class="td-dept-sub">${allMembers.length} member${allMembers.length !== 1 ? 's' : ''}</div>
                </div>
            </div>
            <div class="td-dept-stats">
                <div class="td-dept-stat"><div class="td-dept-stat-num" style="color:#4f46e5">${total}</div><div class="td-dept-stat-lbl">Total</div></div>
                <div class="td-dept-stat"><div class="td-dept-stat-num" style="color:#f59e0b">${pending}</div><div class="td-dept-stat-lbl">Pending</div></div>
                <div class="td-dept-stat"><div class="td-dept-stat-num" style="color:#3b82f6">${inProg}</div><div class="td-dept-stat-lbl">In Progress</div></div>
                <div class="td-dept-stat"><div class="td-dept-stat-num" style="color:#10b981">${completed}</div><div class="td-dept-stat-lbl">Done</div></div>
                <div class="td-dept-stat"><div class="td-dept-stat-num" style="color:#ef4444">${overdue}</div><div class="td-dept-stat-lbl">Overdue</div></div>
            </div>
            <div class="td-dept-prog-section">
                <div class="td-dept-prog-head">
                    <div class="td-dept-prog-lbl">Department Progress</div>
                    <div class="td-dept-prog-text">${pct}%</div>
                </div>
                <div class="td-dept-prog-bar"><div class="td-dept-prog-fill" style="width:${pct}%"></div></div>
            </div>
            <div class="td-dept-employees" data-members='${membersJson}'>
                ${allMembers.map(function (m) {
            const s = getEmpStats(m.employeeId);
            const mt = getValue(s, 'totalTasks', 'TotalTasks');
            const mc = getValue(s, 'completedCount', 'CompletedCount');
            const mp = getEmployeeProgress(m.employeeId, mt, mc);
            const avatarBg = m.role === 'Sub HOD' ? '#4f46e5' : '#0891b2';
            const roleColor = m.role === 'Sub HOD' ? 'background:#ede9fe;color:#4f46e5' : 'background:#e0f2fe;color:#0284c7';
            const initials = m.employeeName.split(' ').map(function (n) { return n[0]; }).join('').substring(0, 2).toUpperCase();
            return `<div class="td-emp-row" onclick="event.stopPropagation();openEmpTasksModal(${m.employeeId},'${m.employeeName.replace(/'/g, "\\'")}')">
                        <div class="td-emp-avatar" style="background:${avatarBg}">${initials}</div>
                        <div class="td-emp-name">${esc(m.employeeName)}</div>
                        <span class="td-emp-role-tag" style="${roleColor}">${m.role}</span>
                        <div class="td-emp-prog-wrap">
                            <div class="td-emp-prog-bar"><div class="td-emp-prog-fill" style="width:${mp}%"></div></div>
                            <span class="td-emp-prog">${mp}%</span>
                        </div>
                    </div>`;
        }).join('')}
            </div>
        </div>`;
    });

    html += '</div>';
    section.innerHTML = html;
}

function toggleDeptEmployees(card) {
    const empDiv = card.querySelector('.td-dept-employees');
    if (!empDiv) return;
    document.querySelectorAll('.td-dept-employees.open').forEach(function (openDiv) {
        if (openDiv !== empDiv) openDiv.classList.remove('open');
    });
    empDiv.classList.toggle('open');
}

// ============================================
// EMPLOYEE TASKS POPUP (HOD)
// ============================================
async function openEmpTasksModal(empId, empName) {
    document.getElementById('empTasksName').textContent = empName;
    document.getElementById('empTasksSubtitle').textContent = 'Loading tasks...';
    document.getElementById('empTasksBody').innerHTML = '<div style="text-align:center;padding:32px;color:#94a3b8"><i class="fas fa-spinner fa-spin fa-2x"></i></div>';
    document.getElementById('empTasksModal').style.display = 'flex';

    try {
        let tasks = allScopedTeamTasks.filter(function (task) {
            return getValue(task, 'assignedToEmployeeId', 'AssignedToEmployeeId') === empId;
        });

        if (!tasks.length) {
            const res = await fetch(`${API}/GetAllTasks`, {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ page: 1, limit: 1000, employeeId: EMPLOYEE_ID, roleTypeId: ROLE_TYPE_ID, viewMode: 'TEAM' })
            });
            const r = await res.json();
            tasks = filterTasksToKnownTeam(r.data || r.Data || []).filter(function (task) {
                return getValue(task, 'assignedToEmployeeId', 'AssignedToEmployeeId') === empId;
            });
        }

        document.getElementById('empTasksSubtitle').textContent = tasks.length + ' task' + (tasks.length !== 1 ? 's' : '');

        if (!tasks.length) {
            document.getElementById('empTasksBody').innerHTML = '<div style="text-align:center;padding:32px;color:#94a3b8"><i class="fas fa-inbox fa-2x"></i><p style="margin-top:12px">No tasks assigned</p></div>';
            return;
        }

        let html = tasks.map(function (t) {
            const isCompleted = !!getValue(t, 'isCompleted', 'IsCompleted');
            const status = getTextValue(t, 'status', 'Status') || 'PENDING';
            const expectedEndDate = getTextValue(t, 'expectedEndDate', 'ExpectedEndDate');
            const taskType = getTextValue(t, 'taskType', 'TaskType');
            const overdue = !isCompleted && new Date(expectedEndDate) < new Date();
            const sc = isCompleted ? 'completed' : (overdue ? 'overdue' : status.toLowerCase().replace('_', '-'));
            const statusColors = { 'completed': '#10b981', 'overdue': '#ef4444', 'pending': '#f59e0b', 'in-progress': '#3b82f6', 'not-started': '#94a3b8' };
            const sColor = statusColors[sc] || '#94a3b8';
            const pct = getValue(t, 'percentComplete', 'PercentComplete');
            return `<div class="emp-task-item">
                <div class="emp-task-top">
                    <span style="background:${sColor}20;color:${sColor};padding:2px 10px;border-radius:20px;font-size:11px;font-weight:700">${isCompleted ? 'Completed' : (overdue ? 'Overdue' : fmtStatus(status))}</span>
                    <span style="background:#f1f5f9;color:#475569;padding:2px 10px;border-radius:20px;font-size:11px;font-weight:600">${getTextValue(t, 'priority', 'Priority') || 'MEDIUM'}</span>
                    ${taskType === 'ASSIGNED' ? '<span style="background:#ede9fe;color:#4f46e5;padding:2px 10px;border-radius:20px;font-size:11px;font-weight:600">Assigned</span>' : '<span style="background:#f0fdf4;color:#16a34a;padding:2px 10px;border-radius:20px;font-size:11px;font-weight:600">Self</span>'}
                </div>
                <div class="emp-task-name">${esc(getTextValue(t, 'taskName', 'TaskName'))}</div>
                <div class="emp-task-meta">
                    <span><i class="fas fa-calendar-plus"></i> ${fmtDate(getTextValue(t, 'startDate', 'StartDate'))}</span>
                    <span class="${overdue ? 'td-text-danger' : ''}"><i class="fas fa-calendar-check"></i> ${fmtDate(expectedEndDate)}</span>
                </div>
                <div class="emp-task-prog">
                    <div class="emp-task-prog-bar"><div class="emp-task-prog-fill" style="width:${pct}%"></div></div>
                    <span style="font-size:12px;font-weight:700;color:#4f46e5;min-width:36px">${pct}%</span>
                </div>
            </div>`;
        }).join('');
        document.getElementById('empTasksBody').innerHTML = html;
    } catch (e) {
        document.getElementById('empTasksBody').innerHTML = '<div style="color:#ef4444;padding:16px">Error loading tasks</div>';
    }
}

function closeEmpTasksModal() {
    document.getElementById('empTasksModal').style.display = 'none';
}

// ============================================
// HOD TASK TABLE
// ============================================
function renderAdminHodPanel() {
    const section = document.getElementById('hierarchySection');
    if (!section) return;
    if (subHODCurrentTab !== 'allhod') {
        section.style.display = 'none';
        return;
    }
    section.style.display = 'block';

    if (!allHodMembers.length) {
        section.innerHTML = '<div class="td-section-header"><h2><i class="fas fa-user-tie"></i> All HODs</h2></div><div class="td-empty"><i class="fas fa-users"></i><h3>No HOD Found</h3><p>No HODs are available for this admin view.</p></div>';
        return;
    }

    const cards = allHodMembers.map(function (member) {
        const employeeId = getValue(member, 'employeeId', 'EmployeeId');
        const employeeName = getTextValue(member, 'employeeName', 'EmployeeName');
        const designation = getTextValue(member, 'designation', 'Designation') || 'HOD';
        const memberTasks = allHodTasks.filter(function (task) {
            return String(getValue(task, 'assignedToEmployeeId', 'AssignedToEmployeeId')) === String(employeeId);
        });
        const summary = summarizeTasks(memberTasks);

        return `
            <div class="subhod-emp-card" onclick="filterByMember(${employeeId})">
                <div class="subhod-emp-card-header">
                    <div class="subhod-emp-avatar">${esc((employeeName || 'H').trim().slice(0, 1).toUpperCase())}</div>
                    <div class="subhod-emp-info">
                        <div class="subhod-emp-name">${esc(employeeName)}</div>
                        <div class="subhod-emp-role">${esc(designation)}</div>
                    </div>
                </div>
                <div class="emp-task-meta">
                    <span><strong>Total:</strong> ${summary.total}</span>
                    <span><strong>Pending:</strong> ${summary.pending}</span>
                    <span><strong>Completed:</strong> ${summary.completed}</span>
                </div>
            </div>`;
    }).join('');

    section.innerHTML = '<div class="td-section-header"><h2><i class="fas fa-user-tie"></i> All HODs</h2></div><div class="subhod-team-cards">' + cards + '</div>';
}

function renderHODTable(tasks) {
    indexTasksForDetails(tasks);
    const wrap = document.getElementById('hodTableWrap');
    if (!wrap) return;

    if (!tasks.length) {
        wrap.innerHTML = '<div class="td-empty"><i class="fas fa-inbox"></i><h3>No Tasks Found</h3><p>No team tasks match current filters</p></div>';
        return;
    }

    let rows = tasks.map(function (t, index) {
        const taskId = getTextValue(t, 'taskId', 'TaskId');
        const taskName = getTextValue(t, 'taskName', 'TaskName');
        const assignedTo = getTextValue(t, 'assignedTo', 'AssignedTo');
        const status = getTextValue(t, 'status', 'Status');
        const priority = getTextValue(t, 'priority', 'Priority');
        const taskType = getTextValue(t, 'taskType', 'TaskType');
        const startDate = getTextValue(t, 'startDate', 'StartDate');
        const expectedEndDate = getTextValue(t, 'expectedEndDate', 'ExpectedEndDate');
        const isCompleted = !!getValue(t, 'isCompleted', 'IsCompleted');
        const overdue = !isCompleted && new Date(expectedEndDate) < new Date();
        const statusColors = { 'COMPLETED': '#10b981', 'overdue': '#ef4444', 'PENDING': '#f59e0b', 'IN_PROGRESS': '#3b82f6' };
        const sColor = statusColors[isCompleted ? 'COMPLETED' : (overdue ? 'overdue' : status)] || '#94a3b8';
        const pct = getValue(t, 'percentComplete', 'PercentComplete');
        const actions = buildTaskActions(taskId, pct, true, isCompleted, canEditTaskProgress(t));
        return `<tr>
            <td>${index + 1}</td>
            <td style="max-width:180px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis" title="${esc(taskName)}">${esc(taskName)}</td>
            <td>${esc(t.assignedTo || '—')}</td>
            <td><span style="background:${sColor}20;color:${sColor};padding:2px 10px;border-radius:20px;font-size:11px;font-weight:700;white-space:nowrap">${t.isCompleted ? 'Completed' : (overdue ? 'Overdue' : fmtStatus(t.status))}</span></td>
            <td><span style="font-size:11px;font-weight:600;padding:2px 8px;background:#f1f5f9;border-radius:10px;color:#475569">${t.priority || '—'}</span></td>
            <td style="white-space:nowrap">${fmtDate(t.startDate)}</td>
            <td style="white-space:nowrap" class="${overdue ? 'td-text-danger' : ''}">${fmtDate(t.expectedEndDate)}</td>
            <td>
                <div class="hod-tbl-prog">
                    <div class="hod-tbl-prog-bar"><div class="hod-tbl-prog-fill" style="width:${pct}%"></div></div>
                    <span style="font-size:11px;font-weight:700;color:#4f46e5;min-width:32px">${pct}%</span>
                </div>
            </td>
            <td><div style="display:flex;gap:4px;flex-wrap:wrap">${actions}</div></td>
        </tr>`;
    }).join('');

    wrap.innerHTML = `
    <div class="hod-filter-bar">
        <select id="hodFilterStatus" onchange="applyHODFilters()">
            <option value="">All Status</option>
            <option value="PENDING">Pending</option>
            <option value="IN_PROGRESS">In Progress</option>
            <option value="COMPLETED">Completed</option>
        </select>
        <select id="hodFilterPriority" onchange="applyHODFilters()">
            <option value="">All Priority</option>
            <option value="LOW">Low</option>
            <option value="MEDIUM">Medium</option>
            <option value="HIGH">High</option>
            <option value="CRITICAL">Critical</option>
        </select>
        <select id="hodFilterType" onchange="applyHODFilters()">
            <option value="">All Types</option>
            <option value="SELF">Self</option>
            <option value="ASSIGNED">Assigned</option>
        </select>
        <input type="text" id="hodFilterPerson" placeholder="Search by person..." oninput="applyHODFilters()" style="width:180px" />
        <button class="td-btn-new" style="padding:7px 14px;font-size:12px" onclick="openCreateTaskModal()"><i class="fas fa-plus"></i> Assign Task</button>
    </div>
    <div class="hod-table-wrap">
        <table class="hod-table">
            <thead>
                <tr>
                    <th>#</th><th>Task</th><th>Assigned To</th><th>Status</th><th>Priority</th><th>Start</th><th>Deadline</th><th>Progress</th><th>Actions</th>
                </tr>
            </thead>
            <tbody id="hodTableBody">${rows}</tbody>
        </table>
    </div>`;
}

function applyHODFilters() {
    var status = (document.getElementById('hodFilterStatus') || {}).value || '';
    var priority = (document.getElementById('hodFilterPriority') || {}).value || '';
    var person = ((document.getElementById('hodFilterPerson') || {}).value || '').toLowerCase();
    var rows = document.querySelectorAll('#hodTableBody tr');
    rows.forEach(function (row) {
        var text = row.textContent.toLowerCase();
        var show = true;
        if (status && !text.includes(status.toLowerCase().replace('_', ' '))) show = false;
        if (priority && !text.includes(priority.toLowerCase())) show = false;
        if (person && !text.includes(person)) show = false;
        row.style.display = show ? '' : 'none';
    });
}

// ============================================
// HELPERS
// ============================================
function parseSlot(desc) {
    if (!desc) return { slot: null, descText: '' };
    var m = desc.match(/^\[Slot:([^\]]+)\]\s*(.*)/s);
    if (m) return { slot: m[1], descText: m[2] };
    return { slot: null, descText: desc };
}
function indexTasksForDetails(tasks) {
    taskDetailsLookup = {};
    (Array.isArray(tasks) ? tasks : []).forEach(function (task) {
        var key = getTextValue(task, 'taskId', 'TaskId');
        if (key) taskDetailsLookup[key] = task;
    });
}
function getInlineIcon(name) {
    var icons = {
        view: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 5c5.5 0 9.5 4.5 10.8 6.2a1.3 1.3 0 0 1 0 1.6C21.5 14.5 17.5 19 12 19S2.5 14.5 1.2 12.8a1.3 1.3 0 0 1 0-1.6C2.5 9.5 6.5 5 12 5Zm0 2C8 7 4.8 10.1 3.3 12 4.8 13.9 8 17 12 17s7.2-3.1 8.7-5C19.2 10.1 16 7 12 7Zm0 2.5A2.5 2.5 0 1 1 9.5 12 2.5 2.5 0 0 1 12 9.5Z" fill="currentColor"/></svg>',
        progress: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 19h16v2H2V3h2v16Zm3-4.5 3.2-3.2 2.4 2.4 4.9-4.9 1.4 1.4-6.3 6.3-2.4-2.4L8.4 16 7 14.5Z" fill="currentColor"/></svg>',
        complete: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 2a10 10 0 1 1-7.07 2.93A10 10 0 0 1 12 2Zm4.3 7.3-5.6 5.6-3-3-1.4 1.4 4.4 4.4 7-7-1.4-1.4Z" fill="currentColor"/></svg>',
        reassign: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M7 7h11.2l-2.6-2.6L17 3l5 5-5 5-1.4-1.4L18.2 9H7V7Zm10 10H5.8l2.6 2.6L7 21l-5-5 5-5 1.4 1.4L5.8 15H17v2Z" fill="currentColor"/></svg>',
        delete: '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M9 3h6l1 2h5v2H3V5h5l1-2Zm1 6h2v8h-2V9Zm4 0h2v8h-2V9ZM6 7h12l-1 13a2 2 0 0 1-2 2H9a2 2 0 0 1-2-2L6 7Z" fill="currentColor"/></svg>'
    };
    return icons[name] || '';
}
function buildTaskActions(taskId, pct, canReassign, isCompleted, canProgress) {
    var viewBtn = `<button class="td-btn-sm" type="button" title="View" aria-label="View" onclick="openTaskDetailsModal('${taskId}')">${getInlineIcon('view')}</button>`;
    if (isCompleted) {
        return `<span class="td-completed-label">${getInlineIcon('complete')}Done</span>${viewBtn}`;
    }

    return `
        ${canProgress ? `<button class="td-btn-sm td-btn-progress" type="button" title="Progress" aria-label="Progress" onclick="openProgressModal('${taskId}',${pct})">${getInlineIcon('progress')}</button>` : ''}
        <button class="td-btn-sm td-btn-complete" type="button" title="Complete" aria-label="Complete" onclick="completeTask('${taskId}')">${getInlineIcon('complete')}</button>
        ${canReassign ? `<button class="td-btn-sm td-btn-reassign" type="button" title="Reassign" aria-label="Reassign" onclick="openReassignModal('${taskId}')">${getInlineIcon('reassign')}</button>` : ''}
        <button class="td-btn-sm td-btn-delete" type="button" title="Delete" aria-label="Delete" onclick="deleteTask('${taskId}')">${getInlineIcon('delete')}</button>
        ${viewBtn}
    `;
}
function getDetailValue(task) {
    for (var i = 1; i < arguments.length; i++) {
        var key = arguments[i];
        if (task && task[key] !== undefined && task[key] !== null && task[key] !== '') return task[key];
    }
    return '';
}
function fmtDateTime(s) {
    if (!s || String(s).startsWith('0001')) return 'N/A';
    var d = new Date(s);
    if (isNaN(d.getTime())) return String(s);
    return d.toLocaleString('en-IN', {
        day: '2-digit',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}
function findTaskById(taskId) {
    if (taskDetailsLookup && taskDetailsLookup[taskId]) return taskDetailsLookup[taskId];
    var sources = [lastOwnTasks, allScopedTeamTasks, allTeamTasks];
    for (var i = 0; i < sources.length; i++) {
        var list = Array.isArray(sources[i]) ? sources[i] : [];
        for (var j = 0; j < list.length; j++) {
            if (getTextValue(list[j], 'taskId', 'TaskId') === taskId) return list[j];
        }
    }
    return null;
}
function getValue(obj) {
    if (!obj) return 0;
    for (var i = 1; i < arguments.length; i++) {
        var key = arguments[i];
        if (obj[key] !== undefined && obj[key] !== null) return obj[key];
    }
    return 0;
}
function getTextValue(obj) {
    if (!obj) return '';
    for (var i = 1; i < arguments.length; i++) {
        var key = arguments[i];
        if (obj[key] !== undefined && obj[key] !== null) return obj[key];
    }
    return '';
}
function buildEmployeeProgressMap(tasks) {
    var progressMap = {};
    tasks.forEach(function (task) {
        var employeeId = getValue(task, 'assignedToEmployeeId', 'AssignedToEmployeeId');
        if (!employeeId) return;
        if (!progressMap[employeeId]) progressMap[employeeId] = { totalProgress: 0, taskCount: 0 };
        progressMap[employeeId].totalProgress += getValue(task, 'percentComplete', 'PercentComplete');
        progressMap[employeeId].taskCount += 1;
    });
    Object.keys(progressMap).forEach(function (employeeId) {
        var item = progressMap[employeeId];
        item.avgProgress = item.taskCount > 0 ? Math.round(item.totalProgress / item.taskCount) : 0;
    });
    return progressMap;
}
function getEmployeeProgress(employeeId, totalTasks, completedTasks) {
    var progressInfo = employeeProgressMap[employeeId];
    if (progressInfo && progressInfo.taskCount > 0) return progressInfo.avgProgress;
    return totalTasks > 0 ? Math.round((completedTasks / totalTasks) * 100) : 0;
}
function getDepartmentProgress(members, totalTasks, completedTasks) {
    var memberCount = 0;
    var totalProgress = 0;
    members.forEach(function (member) {
        var progressInfo = employeeProgressMap[member.employeeId];
        if (!progressInfo || progressInfo.taskCount <= 0) return;
        memberCount += 1;
        totalProgress += progressInfo.avgProgress || 0;
    });
    if (memberCount > 0) return Math.round(totalProgress / memberCount);
    return totalTasks > 0 ? Math.round((completedTasks / totalTasks) * 100) : 0;
}
function renderHODTable(tasks) {
    indexTasksForDetails(tasks);
    const wrap = document.getElementById('hodTableWrap');
    if (!wrap) return;

    if (!tasks.length) {
        wrap.innerHTML = '<div class="td-empty"><i class="fas fa-inbox"></i><h3>No Tasks Found</h3><p>No team tasks match current filters</p></div>';
        return;
    }

    let rows = tasks.map(function (t, index) {
        const taskId = getTextValue(t, 'taskId', 'TaskId');
        const taskName = getTextValue(t, 'taskName', 'TaskName');
        const assignedTo = getTextValue(t, 'assignedTo', 'AssignedTo');
        const status = getTextValue(t, 'status', 'Status');
        const priority = getTextValue(t, 'priority', 'Priority');
        const taskType = getTextValue(t, 'taskType', 'TaskType');
        const startDate = getTextValue(t, 'startDate', 'StartDate');
        const expectedEndDate = getTextValue(t, 'expectedEndDate', 'ExpectedEndDate');
        const isCompleted = !!getValue(t, 'isCompleted', 'IsCompleted');
        const overdue = !isCompleted && new Date(expectedEndDate) < new Date();
        const statusColors = { 'COMPLETED': '#10b981', 'overdue': '#ef4444', 'PENDING': '#f59e0b', 'IN_PROGRESS': '#3b82f6' };
        const sColor = statusColors[isCompleted ? 'COMPLETED' : (overdue ? 'overdue' : status)] || '#94a3b8';
        const pct = getValue(t, 'percentComplete', 'PercentComplete');
        const actions = buildTaskActions(taskId, pct, true, isCompleted, canEditTaskProgress(t));
        return `<tr>
            <td>${index + 1}</td>
            <td style="max-width:180px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis" title="${esc(taskName)}">${esc(taskName)}</td>
            <td>${esc(assignedTo || '-')}</td>
            <td data-status="${esc(status || '')}"><span style="background:${sColor}20;color:${sColor};padding:2px 10px;border-radius:20px;font-size:11px;font-weight:700;white-space:nowrap">${isCompleted ? 'Completed' : (overdue ? 'Overdue' : fmtStatus(status))}</span></td>
            <td data-priority="${esc(priority || '')}"><span style="font-size:11px;font-weight:600;padding:2px 8px;background:#f1f5f9;border-radius:10px;color:#475569">${priority || '-'}</span></td>
            <td style="white-space:nowrap">${fmtDate(startDate)}</td>
            <td style="white-space:nowrap" class="${overdue ? 'td-text-danger' : ''}">${fmtDate(expectedEndDate)}</td>
            <td>
                <div class="hod-tbl-prog">
                    <div class="hod-tbl-prog-bar"><div class="hod-tbl-prog-fill" style="width:${pct}%"></div></div>
                    <span style="font-size:11px;font-weight:700;color:#4f46e5;min-width:32px">${pct}%</span>
                </div>
            </td>
            <td data-type="${esc(taskType || '')}" data-person="${esc(assignedTo || '')}"><div style="display:flex;gap:4px;flex-wrap:wrap">${actions}</div></td>
        </tr>`;
    }).join('');

    wrap.innerHTML = `
    <div class="hod-filter-bar">
        <select id="hodFilterStatus" onchange="applyHODFilters()">
            <option value="">All Status</option>
            <option value="PENDING">Pending</option>
            <option value="IN_PROGRESS">In Progress</option>
            <option value="COMPLETED">Completed</option>
        </select>
        <select id="hodFilterPriority" onchange="applyHODFilters()">
            <option value="">All Priority</option>
            <option value="LOW">Low</option>
            <option value="MEDIUM">Medium</option>
            <option value="HIGH">High</option>
            <option value="CRITICAL">Critical</option>
        </select>
        <select id="hodFilterType" onchange="applyHODFilters()">
            <option value="">All Types</option>
            <option value="SELF">Self</option>
            <option value="ASSIGNED">Assigned</option>
        </select>
        <input type="text" id="hodFilterPerson" placeholder="Search by person..." oninput="applyHODFilters()" style="width:180px" />
        <button class="td-btn-new" style="padding:7px 14px;font-size:12px" onclick="openCreateTaskModal()"><i class="fas fa-plus"></i> Assign Task</button>
    </div>
    <div class="hod-table-wrap">
        <table class="hod-table">
            <thead>
                <tr>
                    <th>#</th><th>Task</th><th>Assigned To</th><th>Status</th><th>Priority</th><th>Start</th><th>Deadline</th><th>Progress</th><th>Actions</th>
                </tr>
            </thead>
            <tbody id="hodTableBody">${rows}</tbody>
        </table>
    </div>`;
}
function applyHODFilters() {
    var status = (document.getElementById('hodFilterStatus') || {}).value || '';
    var priority = (document.getElementById('hodFilterPriority') || {}).value || '';
    var type = (document.getElementById('hodFilterType') || {}).value || '';
    var person = ((document.getElementById('hodFilterPerson') || {}).value || '').toLowerCase();
    var rows = document.querySelectorAll('#hodTableBody tr');
    rows.forEach(function (row) {
        var rowStatus = ((row.querySelector('[data-status]') || {}).dataset || {}).status || '';
        var rowPriority = ((row.querySelector('[data-priority]') || {}).dataset || {}).priority || '';
        var actionsCell = row.querySelector('[data-type]');
        var rowType = actionsCell ? (actionsCell.dataset.type || '') : '';
        var rowPerson = actionsCell ? (actionsCell.dataset.person || '').toLowerCase() : '';
        var show = true;
        if (status && rowStatus !== status) show = false;
        if (priority && rowPriority !== priority) show = false;
        if (type && rowType !== type) show = false;
        if (person && !rowPerson.includes(person)) show = false;
        row.style.display = show ? '' : 'none';
    });
}
// ============================================
// SUB-HOD: TEAM LOADING SKELETON (RIGHT PANEL)
// ============================================
function renderSubHODTeamLoading() {
    const section = document.getElementById('hierarchySection');
    if (!section || ROLE_TYPE_ID !== 2) return;
    section.style.display = 'block';
    let cards = '';
    for (let i = 0; i < 4; i++) {
        cards += `
        <div class="td-dept-card loading">
            <div class="td-dept-header">
                <div class="td-dept-skeleton td-dept-skeleton-icon"></div>
                <div style="flex:1">
                    <div class="td-dept-skeleton td-dept-skeleton-title"></div>
                    <div class="td-dept-skeleton td-dept-skeleton-sub"></div>
                </div>
            </div>
            <div class="td-dept-stats">
                <div class="td-dept-stat"><div class="td-dept-skeleton td-dept-skeleton-stat"></div></div>
                <div class="td-dept-stat"><div class="td-dept-skeleton td-dept-skeleton-stat"></div></div>
                <div class="td-dept-stat"><div class="td-dept-skeleton td-dept-skeleton-stat"></div></div>
                <div class="td-dept-stat"><div class="td-dept-skeleton td-dept-skeleton-stat"></div></div>
                <div class="td-dept-stat"><div class="td-dept-skeleton td-dept-skeleton-stat"></div></div>
            </div>
            <div class="td-dept-prog-section">
                <div class="td-dept-prog-head">
                    <div class="td-dept-skeleton td-dept-skeleton-sub" style="width:80px"></div>
                    <div class="td-dept-skeleton td-dept-skeleton-pct"></div>
                </div>
                <div class="td-dept-skeleton td-dept-skeleton-progress"></div>
            </div>
        </div>`;
    }
    section.innerHTML = '<div class="td-section-header"><h2><i class="fas fa-users"></i> My Team</h2></div><div class="td-dept-grid" style="flex-direction:column">' + cards + '</div>';
}

// ============================================
// SUB-HOD: TEAM MEMBER CARDS (RIGHT PANEL)
// ============================================
function renderSubHODTeamPanel() {
    const section = document.getElementById('hierarchySection');
    if (!section) return;
    section.style.display = 'block';

    if (!breakdownData.length) {
        section.innerHTML = '<div class="td-section-header"><h2><i class="fas fa-users"></i> My Team</h2></div><div style="text-align:center;padding:32px;color:#94a3b8"><i class="fas fa-users fa-2x"></i><p style="margin-top:12px">No team members found</p></div>';
        return;
    }

    // Use same card structure & CSS classes as HOD dept cards for visual consistency
    const bgColors = ['#4f46e5', '#0891b2', '#059669', '#d97706', '#dc2626', '#7c3aed', '#0284c7', '#16a34a'];

    let html = '<div class="td-section-header"><h2><i class="fas fa-users"></i> My Team</h2></div><div class="td-dept-grid">';

    breakdownData.forEach(function (m, idx) {
        const empId = getValue(m, 'employeeId', 'EmployeeId');
        const empName = getTextValue(m, 'employeeName', 'EmployeeName');
        const total = getValue(m, 'totalTasks', 'TotalTasks');
        const done = getValue(m, 'completedCount', 'CompletedCount');
        const pending = getValue(m, 'pendingCount', 'PendingCount');
        const inProg = getValue(m, 'inProgressCount', 'InProgressCount');
        const overdue = getValue(m, 'overdueCount', 'OverdueCount');
        const pct = getEmployeeProgress(empId, total, done);
        const initials = empName.split(' ').filter(Boolean).map(function (n) { return n[0]; }).join('').substring(0, 2).toUpperCase() || '?';
        const roleTypeId = getValue(m, 'roleTypeId', 'RoleTypeId');
        const roleLabel = { 1: 'HOD', 2: 'Sub HOD', 3: 'Executive' }[roleTypeId] || 'Executive';
        const bg = bgColors[idx % bgColors.length];
        const safeName = empName.replace(/\\/g, '\\\\').replace(/'/g, "\\'");

        html += `
        <div class="td-dept-card" onclick="openEmpTasksModal(${empId},'${safeName}')">
            <div class="td-dept-header">
                <div class="td-dept-icon" style="background:${bg};font-size:14px;font-weight:800">${esc(initials)}</div>
                <div>
                    <div class="td-dept-title">${esc(empName)}</div>
                    <div class="td-dept-sub">${roleLabel}</div>
                </div>
            </div>
            <div class="td-dept-stats">
                <div class="td-dept-stat">
                    <div class="td-dept-stat-num" style="color:#4f46e5">${total}</div>
                    <div class="td-dept-stat-lbl">Total</div>
                </div>
                <div class="td-dept-stat">
                    <div class="td-dept-stat-num" style="color:#f59e0b">${pending}</div>
                    <div class="td-dept-stat-lbl">Pending</div>
                </div>
                <div class="td-dept-stat">
                    <div class="td-dept-stat-num" style="color:#3b82f6">${inProg}</div>
                    <div class="td-dept-stat-lbl">In Prog</div>
                </div>
                <div class="td-dept-stat">
                    <div class="td-dept-stat-num" style="color:#10b981">${done}</div>
                    <div class="td-dept-stat-lbl">Done</div>
                </div>
                <div class="td-dept-stat">
                    <div class="td-dept-stat-num" style="color:#ef4444">${overdue}</div>
                    <div class="td-dept-stat-lbl">Overdue</div>
                </div>
            </div>
            <div class="td-dept-prog-section">
                <div class="td-dept-prog-head">
                    <div class="td-dept-prog-lbl">Progress</div>
                    <div class="td-dept-prog-text">${pct}%</div>
                </div>
                <div class="td-dept-prog-bar">
                    <div class="td-dept-prog-fill" style="width:${pct}%"></div>
                </div>
            </div>
        </div>`;
    });

    html += '</div>';
    section.innerHTML = html;
}

// ============================================
// SUB-HOD: TASK LIST (LEFT PANEL — latest 10)
// ============================================
function renderSubHODTaskList(tasks) {
    indexTasksForDetails(tasks);
    const wrap = document.getElementById('hodTableWrap');
    if (!wrap) return;

    // Exclude Sub-HOD's own tasks from team view (safety net in case SP doesn't filter)
    tasks = tasks.filter(function (t) {
        var assignedId = getValue(t, 'assignedToEmployeeId', 'AssignedToEmployeeId');
        return assignedId !== EMPLOYEE_ID;
    });

    const headerHtml = `
    <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:14px;flex-wrap:wrap;gap:10px;padding-bottom:12px;border-bottom:1px solid #f1f5f9;">
        <div style="display:flex;align-items:center;gap:8px;">
            <div style="width:32px;height:32px;background:linear-gradient(135deg,#4f46e5,#7c3aed);border-radius:8px;display:flex;align-items:center;justify-content:center;">
                <i class="fas fa-users" style="color:#fff;font-size:14px"></i>
            </div>
            <div>
                <div style="font-size:15px;font-weight:700;color:#1e293b;">Team Tasks</div>
                <div style="font-size:11px;color:#94a3b8;">All scoped team tasks</div>
            </div>
        </div>
        <div style="display:flex;gap:8px;flex-wrap:wrap;">
            <select id="subhodFilterStatus" onchange="applySubHODFilters()" style="border:1.5px solid #e2e8f0;border-radius:8px;padding:6px 12px;font-size:12px;font-weight:500;color:#374151;background:#fff;outline:none;cursor:pointer;">
                <option value="">All Status</option>
                <option value="PENDING">Pending</option>
                <option value="IN_PROGRESS">In Progress</option>
                <option value="COMPLETED">Completed</option>
            </select>
            <select id="subhodFilterPriority" onchange="applySubHODFilters()" style="border:1.5px solid #e2e8f0;border-radius:8px;padding:6px 12px;font-size:12px;font-weight:500;color:#374151;background:#fff;outline:none;cursor:pointer;">
                <option value="">All Priority</option>
                <option value="LOW">Low</option>
                <option value="MEDIUM">Medium</option>
                <option value="HIGH">High</option>
                <option value="CRITICAL">Critical</option>
            </select>
        </div>
    </div>`;

    if (!tasks.length) {
        wrap.innerHTML = headerHtml + '<div class="td-empty"><i class="fas fa-inbox"></i><h3>No Tasks Found</h3><p>Create a new task to get started</p></div>';
        return;
    }

    const cardsHtml = tasks.map(function (t) {
        const taskId = getTextValue(t, 'taskId', 'TaskId');
        const taskName = getTextValue(t, 'taskName', 'TaskName');
        const description = getTextValue(t, 'description', 'Description');
        const assignedTo = getTextValue(t, 'assignedTo', 'AssignedTo') || 'Unassigned';
        const assignedByName = getTextValue(t, 'assignedByName', 'AssignedByName');
        const expectedEndDate = getTextValue(t, 'expectedEndDate', 'ExpectedEndDate');
        const startDate = getTextValue(t, 'startDate', 'StartDate');
        const priority = getTextValue(t, 'priority', 'Priority') || 'MEDIUM';
        const taskType = getTextValue(t, 'taskType', 'TaskType');
        const status = getTextValue(t, 'status', 'Status') || 'PENDING';
        const isCompleted = !!getValue(t, 'isCompleted', 'IsCompleted');
        const pct = getValue(t, 'percentComplete', 'PercentComplete');
        const overdue = !isCompleted && new Date(expectedEndDate) < new Date();
        const sc = isCompleted ? 'completed' : (overdue ? 'overdue' : status.toLowerCase());
        const slot = getTextValue(t, 'slot', 'Slot');
        const parsedTaskDetails = parseSlot(description);
        const descText = parsedTaskDetails.descText;
        const displaySlot = slot || parsedTaskDetails.slot;
        return `
        <div class="td-task-card" data-task-id="${taskId}" data-status="${status}" data-priority="${priority}">
            <div class="td-task-top">
                <div class="td-task-left">
                    <span class="td-priority td-priority-${priority.toLowerCase()}">${priority}</span>
                    <span class="td-task-type ${taskType === 'ASSIGNED' ? 'td-type-assigned' : 'td-type-self'}">${taskType === 'ASSIGNED' ? 'Assigned' : 'Self'}</span>
                    <span class="td-status td-status-${sc}">${isCompleted ? 'Completed' : (overdue ? 'Overdue' : fmtStatus(status))}</span>
                    ${displaySlot ? `<span class="td-timeslot-badge"><i class="fas fa-clock"></i> ${esc(displaySlot)}</span>` : ''}
                </div>
                <div class="td-task-right"><span class="td-pct">${pct}%</span></div>
            </div>
            <h3 class="td-task-title">${esc(taskName)}</h3>
            ${descText ? `<p class="td-task-desc">${esc(descText)}</p>` : ''}
            <div class="td-task-meta">
                <span><i class="fas fa-user-circle"></i> ${esc(assignedTo)}</span>
                ${assignedByName ? `<span><i class="fas fa-user-tag"></i> By: ${esc(assignedByName)}</span>` : ''}
            </div>
            <div class="td-progress-bar"><div class="td-progress-fill ${pct >= 100 ? 'td-pf-done' : ''}" style="width:${pct}%"></div></div>
            <div class="td-task-dates">
                <span><i class="fas fa-calendar-plus"></i> ${fmtDate(startDate)}</span>
                <span class="${overdue ? 'td-text-danger' : ''}"><i class="fas fa-calendar-check"></i> ${fmtDate(expectedEndDate)}</span>
            </div>
            <div class="td-task-actions">${buildTaskActions(taskId, pct, true, isCompleted, canEditTaskProgress(t))}</div>
        </div>`;
    }).join('');

    wrap.innerHTML = headerHtml + `<div id="subhodTaskCards" class="td-task-list" style="max-height:70vh;overflow-y:auto;padding-right:4px">${cardsHtml}</div>`;
}

function applySubHODFilters() {
    const status = (document.getElementById('subhodFilterStatus') || {}).value || '';
    const priority = (document.getElementById('subhodFilterPriority') || {}).value || '';
    document.querySelectorAll('#subhodTaskCards .td-task-card').forEach(function (card) {
        const show = (!status || card.dataset.status === status) && (!priority || card.dataset.priority === priority);
        card.style.display = show ? '' : 'none';
    });
}

function closeTaskDetailsModal() {
    var modal = document.getElementById('taskDetailsModal');
    if (modal) modal.style.display = 'none';
}

async function openTaskDetailsModal(taskId) {
    var modal = document.getElementById('taskDetailsModal');
    var body = document.getElementById('taskDetailsBody');
    if (!modal || !body) return;

    body.innerHTML = '<div style="text-align:center;padding:32px;color:#94a3b8"><i class="fas fa-spinner fa-spin fa-2x"></i></div>';
    modal.style.display = 'flex';

    try {
        var detailRes = await fetch(`${API}/GetTaskDetails/${encodeURIComponent(taskId)}`);
        var detailJson = await detailRes.json().catch(function () { return null; });

        if (!detailRes.ok || !(detailJson && (detailJson.success || detailJson.Success)) || !(detailJson.data || detailJson.Data)) {
            body.innerHTML = '<p style="text-align:center;color:#ef4444;padding:32px;">Failed to load task details.</p>';
            return;
        }

        var payload = detailJson.data || detailJson.Data;
        renderTaskDetailsModal(payload.task || payload.Task || {});
    } catch (e) {
        console.error('Task details load error:', e);
        body.innerHTML = '<p style="text-align:center;color:#ef4444;padding:32px;">Error loading task details.</p>';
    }
}

function renderTaskDetailsModal(task) {
    var body = document.getElementById('taskDetailsBody');
    if (!body) return;

    var taskId = getDetailValue(task, 'task_Id', 'Task_Id', 'taskId', 'TaskId') || '-';
    var taskName = getDetailValue(task, 'task_Name', 'Task_Name', 'taskName', 'TaskName') || '-';
    var description = getDetailValue(task, 'description', 'Description') || '-';
    var projectName = getDetailValue(task, 'project_Name', 'Project_Name', 'projectName', 'ProjectName') || '-';
    var moduleName = getDetailValue(task, 'module_Name', 'Module_Name', 'moduleName', 'ModuleName') || '-';
    var assignedTo = getDetailValue(task, 'assignedName', 'AssignedName', 'assignedTo', 'AssignedTo') || '-';
    var createdBy = getDetailValue(task, 'created_By', 'Created_By', 'createdBy', 'CreatedBy') || '-';
    var assignedBy = getDetailValue(task, 'assignedByName', 'AssignedByName') || '-';
    var status = getDetailValue(task, 'status', 'Status') || '-';
    var priority = getDetailValue(task, 'priority', 'Priority') || '-';
    var slot = getDetailValue(task, 'slot', 'Slot') || '-';
    var remarks = getDetailValue(task, 'remarks', 'Remarks') || '-';
    var percentComplete = getDetailValue(task, 'percentComplete', 'PercentComplete') || 0;
    var taskType = getDetailValue(task, 'taskType', 'TaskType') || '-';
    var createdOn = fmtDateTime(getDetailValue(task, 'created_On', 'Created_On', 'createdOn', 'CreatedOn'));
    var startDate = fmtDateTime(getDetailValue(task, 'start_Date', 'Start_Date', 'startDate', 'StartDate'));
    var expectedEndDate = fmtDateTime(getDetailValue(task, 'expected_End_Date', 'Expected_End_Date', 'expectedEndDate', 'ExpectedEndDate'));
    var completionDate = fmtDateTime(getDetailValue(task, 'completion_Date', 'Completion_Date', 'completionDate', 'CompletionDate'));
    var lastModifiedBy = getDetailValue(task, 'lastModifiedBy', 'LastModifiedBy') || '-';
    var lastModifiedOn = fmtDateTime(getDetailValue(task, 'lastModifiedOn', 'LastModifiedOn'));

    body.innerHTML = `
        <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:14px;">
            <div style="padding:14px;border:1px solid #e2e8f0;border-radius:14px;background:#f8fafc;"><div style="font-size:12px;color:#64748b;">Task ID</div><div style="font-weight:700;color:#1e293b;">${esc(String(taskId))}</div></div>
            <div style="padding:14px;border:1px solid #e2e8f0;border-radius:14px;background:#f8fafc;"><div style="font-size:12px;color:#64748b;">Status</div><div style="font-weight:700;color:#1e293b;">${esc(fmtStatus(String(status).toUpperCase()))}</div></div>
            <div style="padding:14px;border:1px solid #e2e8f0;border-radius:14px;background:#f8fafc;"><div style="font-size:12px;color:#64748b;">Priority</div><div style="font-weight:700;color:#1e293b;">${esc(String(priority))}</div></div>
            <div style="padding:14px;border:1px solid #e2e8f0;border-radius:14px;background:#f8fafc;"><div style="font-size:12px;color:#64748b;">Progress</div><div style="font-weight:700;color:#1e293b;">${esc(String(percentComplete))}%</div></div>
        </div>
        <div style="margin-top:16px;padding:16px;border:1px solid #e2e8f0;border-radius:16px;background:#fff;">
            <h3 style="margin:0 0 12px;color:#0f172a;font-size:16px;">Task Information</h3>
            <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:14px;">
                <div><div style="font-size:12px;color:#64748b;">Task Name</div><div style="font-weight:600;color:#1e293b;">${esc(String(taskName))}</div></div>
                <div><div style="font-size:12px;color:#64748b;">Task Type</div><div style="font-weight:600;color:#1e293b;">${esc(String(taskType))}</div></div>
                <div><div style="font-size:12px;color:#64748b;">Project</div><div style="font-weight:600;color:#1e293b;">${esc(String(projectName))}</div></div>
                <div><div style="font-size:12px;color:#64748b;">Module</div><div style="font-weight:600;color:#1e293b;">${esc(String(moduleName))}</div></div>
                <div><div style="font-size:12px;color:#64748b;">Assigned To</div><div style="font-weight:600;color:#1e293b;">${esc(String(assignedTo))}</div></div>
                <div><div style="font-size:12px;color:#64748b;">Assigned By</div><div style="font-weight:600;color:#1e293b;">${esc(String(assignedBy))}</div></div>
                <div><div style="font-size:12px;color:#64748b;">Created By</div><div style="font-weight:600;color:#1e293b;">${esc(String(createdBy))}</div></div>
                <div><div style="font-size:12px;color:#64748b;">Slot</div><div style="font-weight:600;color:#1e293b;">${esc(String(slot))}</div></div>
                <div><div style="font-size:12px;color:#64748b;">Created On</div><div style="font-weight:600;color:#1e293b;">${esc(createdOn)}</div></div>
                <div><div style="font-size:12px;color:#64748b;">Start Date</div><div style="font-weight:600;color:#1e293b;">${esc(startDate)}</div></div>
                <div><div style="font-size:12px;color:#64748b;">Expected End</div><div style="font-weight:600;color:#1e293b;">${esc(expectedEndDate)}</div></div>
                <div><div style="font-size:12px;color:#64748b;">Completed On</div><div style="font-weight:600;color:#1e293b;">${esc(completionDate)}</div></div>
                <div><div style="font-size:12px;color:#64748b;">Last Modified By</div><div style="font-weight:600;color:#1e293b;">${esc(String(lastModifiedBy))}</div></div>
                <div><div style="font-size:12px;color:#64748b;">Last Modified On</div><div style="font-weight:600;color:#1e293b;">${esc(lastModifiedOn)}</div></div>
            </div>
            <div style="margin-top:14px;"><div style="font-size:12px;color:#64748b;">Description</div><div style="margin-top:4px;color:#334155;line-height:1.6;">${esc(String(description))}</div></div>
            <div style="margin-top:14px;"><div style="font-size:12px;color:#64748b;">Remarks</div><div style="margin-top:4px;color:#334155;line-height:1.6;">${esc(String(remarks))}</div></div>
        </div>
    `;
}
window.openTaskDetailsModal = openTaskDetailsModal;
window.closeTaskDetailsModal = closeTaskDetailsModal;

function fmtStatus(s) { return { PENDING: 'Pending', IN_PROGRESS: 'In Progress', COMPLETED: 'Completed', NOT_STARTED: 'Not Started' }[s] || s || 'Pending'; }
function fmtDate(s) { if (!s || s.startsWith('0001')) return 'N/A'; return new Date(s).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' }); }
function esc(t) { if (!t) return ''; const d = document.createElement('div'); d.textContent = t; return d.innerHTML; }
function showLoader() { document.getElementById('loaderOverlay').style.display = 'flex'; }
function hideLoader() { document.getElementById('loaderOverlay').style.display = 'none'; }
function showNotification(msg, type) {
    const el = document.createElement('div');
    el.className = `td-notif td-notif-${type}`;
    el.innerHTML = `<i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i> ${msg}`;
    document.body.appendChild(el);
    setTimeout(() => el.classList.add('show'), 50);
    setTimeout(() => { el.classList.remove('show'); setTimeout(() => el.remove(), 300); }, 3000);
}
window.addEventListener('click', e => {
    ['createTaskModal', 'progressModal', 'reassignModal', 'empTasksModal', 'taskDetailsModal'].forEach(id => {
        if (e.target === document.getElementById(id)) document.getElementById(id).style.display = 'none';
    });
});
