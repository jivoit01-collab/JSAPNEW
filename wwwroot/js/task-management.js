let allTask = [];
const API_BASE_URL = '/api/Task';
const CURRENT_USER = String(userId); // Replace with actual logged-in user
const priorityMapping = [
    { priority: 'LOW', weight: 0.5 },
    { priority: 'MEDIUM', weight: 0.75 },
    { priority: 'HIGH', weight: 1.00 },
    { priority: 'CRITICAL', weight: 2.00 }
];


function getWeightage(priorityName) {
    if (!priorityName) {
        // Default to MEDIUM if no priority is set
        return { priority: 'MEDIUM', weight: 0.75 };
    }
    const weightage = priorityMapping.find(p => p.priority === priorityName.toUpperCase())
    return weightage || { priority: 'MEDIUM', weight: 0.75 }; // Default fallback
}
function getCompletedWeightage(data) {

    let completedWeightageArray = []
    let completedWeightage = 0;

    const completedTask = data.filter((task) => task.isCompleted == true)
    completedTask.forEach(task => {
        completedWeightageArray.push(getWeightage(task.priority))
    })


    completedWeightageArray.forEach(cw => {
        completedWeightage = completedWeightage + cw.weight
    })

    return completedWeightage
}


function getTotalWeightage(data) {

    let totalWeightageArray = []
    let totalWeightage = 0;

    data.forEach(task => {
        totalWeightageArray.push(getWeightage(task.priority))
    })


    totalWeightageArray.forEach(tw => {
        totalWeightage = totalWeightage + tw.weight
    })

    return totalWeightage;
}

function completePercentage(data) {
    // Handle empty data or no tasks
    if (!data || data.length === 0) {
        console.log('No tasks available');
        return '0%';
    }

    const completed = getCompletedWeightage(data)
    const total = getTotalWeightage(data)
    console.log('Completed weightage:', completed)
    console.log('Total weightage:', total)

    // Handle division by zero
    if (total === 0) {
        return '0%';
    }

    return `${Math.round((completed / total) * 100)}%`
}
async function calculateCompletePercentage() {
    await getAllTasks();
    const bar = document.getElementsByClassName('bar')[0];
    const percentageText = document.querySelector('.percentage-text');


    if (bar) {
        // Check if there are any tasks
        if (!allTask || allTask.length === 0) {
            bar.style.width = '0%';
            if (percentageText) {
                percentageText.textContent = '0%';
            }
            const taskInfo = document.querySelector('.task-info');
            if (taskInfo) {
                taskInfo.textContent = 'No tasks yet. Create your first task!';
            }
            console.log('Completion: 0% (no tasks)');
            return;
        }

        const percentage = completePercentage(allTask);

        // Animate the bar width
        setTimeout(() => {
            bar.style.width = percentage;
        }, 100);

        // Update percentage text
        if (percentageText) {
            const numericValue = parseInt(percentage);
            percentageText.textContent = percentage;

            // Change color based on completion
            if (numericValue === 100) {
                bar.style.background = 'linear-gradient(90deg, #f59e0b 0%, #f97316 100%)';
                document.querySelector('.task-info').textContent = '🎉 All tasks completed!';
            } else if (numericValue >= 75) {
                document.querySelector('.task-info').textContent = 'Almost there! Keep it up!';
            } else if (numericValue >= 50) {
                document.querySelector('.task-info').textContent = 'Great progress! You\'re halfway there!';
            }
        }

        console.log('Completion:', percentage);
    }

}
// ============================================
// FETCH ALL TASKS
// ============================================

// Default filter configuration
function getDefaultFilter(overrides = {}) {
    return {
        page: 1,
        limit: 20,
        status: null,
        priority: null,
        projectName: null,
        moduleName: null,
        assignedTo: null,
        createdBy: CURRENT_USER,  // Uses the logged-in user ID
        deptId: null,
        sortBy: "created_on",
        sortOrder: "DESC",
        ...overrides  // Allow overriding any default values
    };
}

async function getAllTasks(filterOverrides = {}) {
    try {
        showLoader();

        // Build filter with defaults and any overrides
        const filter = getDefaultFilter(filterOverrides);

        console.log('📤 Fetching tasks with filter:', filter);

        const response = await fetch(`${API_BASE_URL}/GetAllTasks`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(filter)
        });

        const result = await response.json();
        console.log('📥 GetAllTasks response:', result);

        if (result.success) {
            // FIXED: API returns data as array directly, not as { tasks: [...] }
            allTask = Array.isArray(result.data) ? result.data : (result.data.tasks || []);
            updateStats();
            return allTask;
        } else {
            console.error('API returned success: false', result);
            allTask = [];
            return [];
        }
    } catch (error) {
        console.error('Error fetching tasks:', error);
        showNotification('Failed to load tasks', 'error');
        return [];
    } finally {
        hideLoader();
    }
}

// ============================================
// CREATE TASK CARDS
// ============================================
async function createTaskCards(filterOverrides = {}) {
    await getAllTasks(filterOverrides);

    const taskContainer = document.getElementsByClassName("task-container")[0];

    if (!taskContainer) {
        console.error('task-container not found!');
        return;
    }

    taskContainer.innerHTML = '';

    if (!allTask || allTask.length === 0) {
        taskContainer.innerHTML = `
            <div class="no-tasks">
                <i class="fas fa-inbox"></i>
                <h3>No Tasks Found</h3>
                <p>Create your first task to get started!</p>

            </div>
        `;
        return;
    }

    allTask.forEach(task => {
        const taskCard = document.createElement("div");
        taskCard.className = "task-card";

        const isCompleted = task.isCompleted;
        const isOverdue = task.status === 'OVERDUE';
        taskCard.innerHTML = `
            <div class="task-card-header">
                <div class="task-id-section">
                    <span class="task-id-badge">${task.taskId}</span>
                    <span class="priority-badge priority-${(task.priority || 'MEDIUM').toLowerCase()}">
                        <i class="fas fa-flag"></i> ${task.priority || 'MEDIUM'}
                    </span>
                </div>
                <div class="task-status-badge status-${getStatusClass(task.status)}">
                    ${getStatusIcon(task.status)} ${formatStatus(task.status)}
                </div>
            </div>

            <div class="task-card-body">
                <h3 class="task-title">${escapeHtml(task.taskName)}</h3>
                <p class="task-description">${escapeHtml(task.description) || '<em>No description provided</em>'}</p>
                
                <div class="task-meta-info">
                     <div class="meta-item">
                        <i class="fas fa-project-diagram"></i>
                        <span><strong>Dept:</strong> ${escapeHtml(task.deptName)}</span>
                    </div>
                    <div class="meta-item">
                        <i class="fas fa-project-diagram"></i>
                        <span><strong>Project:</strong> ${escapeHtml(task.projectName)}</span>
                    </div>
                    <div class="meta-item">
                        <i class="fas fa-cube"></i>
                        <span><strong>Module:</strong> ${escapeHtml(task.moduleName)}</span>
                    </div>
                    <div class="meta-item">
                        <i class="fas fa-user-circle"></i>
                        <span><strong>Assigned:</strong> ${task.assignedTo == 0 ? "Self Assigned" : escapeHtml(task.assignedTo)}</span>
                    </div>
                    <div class="meta-item">
                        <i class="fas fa-user-edit"></i>
                        <span><strong>Created by:</strong> ${escapeHtml(task.createdBy)}</span>
                    </div>
                </div>

                <div class="task-dates">
                    <div class="date-item">
                        <i class="fas fa-calendar-plus"></i>
                        <div>
                            <small>Start Date</small>
                            <strong>${formatDate(task.startDate)}</strong>
                        </div>
                    </div>
                    <div class="date-item">
                        <i class="fas fa-calendar-check"></i>
                        <div>
                            <small>Due Date</small>
                            <strong class="${isOverdue ? 'text-danger' : ''}">${formatDate(task.expectedEndDate)}</strong>
                            ${task.deadlineExtended ? '<span class="extended-badge"><i class="fas fa-clock"></i> Extended</span>' : ''}
                        </div>
                    </div>
                    ${task.completionDate ? `
                        <div class="date-item">
                            <i class="fas fa-check-circle"></i>
                            <div>
                                <small>Completed</small>
                                <strong>${formatDate(task.completionDate)}</strong>
                            </div>
                        </div>
                    ` : ''}
                </div>

                ${task.deadlineExtended && task.originalExpectedEndDate ? `
                    <div class="deadline-extended-info">
                        <i class="fas fa-info-circle"></i>
                        Original deadline was ${formatDate(task.originalExpectedEndDate)}
                    </div>
                ` : ''}
            </div>

            <div class="task-card-footer">
                <div class="task-timestamps">
                    <small><i class="fas fa-clock"></i> Created: ${formatDateTime(task.createdOn)}</small>
                    <small><i class="fas fa-edit"></i> Modified: ${formatDateTime(task.lastModifiedOn)}</small>
                </div>
                <div class="task-actions">
                    ${!isCompleted ? `
                        <button class="btn-action btn-complete" onclick="completeTask('${task.taskId}')" title="Mark Complete">
                            <i class="fas fa-check-circle"></i> Complete
                        </button>
                    ` : `
                        <button class="btn-action btn-completed-disabled" disabled title="Task Completed">
                            <i class="fas fa-check-circle"></i> Completed
                        </button>
                    `}
                    <button class="btn-action btn-delete" onclick="deleteTask('${task.taskId}')" title="Delete Task">
                        <i class="fas fa-trash-alt"></i> Delete
                    </button>
                </div>
            </div>
        `;

        taskContainer.appendChild(taskCard);
    });
}

// ============================================
// COMPLETE TASK
// ============================================
async function completeTask(taskId) {
    if (!confirm('Are you sure you want to mark this task as completed?')) {
        return;
    }

    try {
        showLoader();

        // FIXED: Changed from PATCH to POST with body matching CompleteTaskRequestDto
        const response = await fetch(`${API_BASE_URL}/CompleteTask`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                taskId: taskId,
                lastModifiedBy: CURRENT_USER
            })
        });

        const result = await response.json();

        if (result.success) {
            showNotification('Task completed successfully!', 'success');
            await createTaskCards(); // Reload cards
        } else {
            showNotification(result.message || 'Failed to complete task', 'error');
        }
    } catch (error) {
        console.error('Error completing task:', error);
        showNotification('Error completing task', 'error');
    } finally {
        hideLoader();
        location.reload()
    }
}

// ============================================
// DELETE TASK
// ============================================
async function deleteTask(taskId) {
    if (!confirm('Are you sure you want to delete this task? This action cannot be undone.')) {
        return;
    }

    try {
        showLoader();

        // FIXED: Changed from DELETE to POST with query parameter
        const response = await fetch(`${API_BASE_URL}/DeleteTask?TaskId=${encodeURIComponent(taskId)}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        });

        const result = await response.json();

        if (result.success) {
            showNotification('Task deleted successfully!', 'success');
            await createTaskCards(); // Reload cards
        } else {
            showNotification(result.message || 'Failed to delete task', 'error');
        }
    } catch (error) {
        console.error('Error deleting task:', error);
        showNotification('Error deleting task', 'error');
    } finally {
        hideLoader();
        location.reload()

    }
}



function populateUsers(usersData) {
    const userDropdown = document.getElementById('assignedTo')
    console.log(usersData)
    console.log(userDropdown)


    userDropdown.innerHTML = '<option value = 0 >Self Assigned</option>'
    usersData.forEach(user => {
        const option = document.createElement('option')
        option.value = user.userId
        option.textContent = user.fullName

        console.log(option)
        userDropdown.appendChild(option)
    })
}

async function getUsers(dept) {

    const deptId = dept.value
    const userGroup = document.getElementsByClassName('user-group')[0]

    const url = `/api/auth/getUsersByDepartment?deptId=${deptId}`;
    try {
        const response = await fetch(url)
        if (!response.ok) return

        const result = await response.json()
        //const data = result.data.users
        const data = result.data?.users || result.data || [];

        if (userGroup.classList.contains('disabled-group')) {
            userGroup.classList.remove('disabled-group')
        }
        console.log('users enabled')
        populateUsers(data)
    } catch (err) {
        console.log(err)
    }

}

async function getDepartment() {
    const url = `/api/auth/getdepartments`
    try {
        const response = await fetch(url)
        if (!response.ok) return;

        const result = await response.json()
        const departments = result.data

        console.log(departments)
        return departments
    } catch (err) {
        console.error(err)
    }
}
// ============================================
// OPEN ADD TASK MODAL
// ============================================
async function openAddTaskModal() {
    const departments = await getDepartment()

    console.log(departments)
    const deptDropDown = document.getElementById("dept")
    console.log(deptDropDown)
    departments.forEach(dept => {
        const option = document.createElement('option')
        option.value = dept.deptId
        option.textContent = dept.deptName

        deptDropDown.appendChild(option)
    })


    document.getElementById('addTaskModal').style.display = 'flex';
    document.getElementById('taskForm').reset();

    // Set default dates
    const today = new Date().toISOString().split('T')[0];
    const nextWeek = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];

    document.getElementById('startDate').value = today;
    document.getElementById('expectedEndDate').value = nextWeek;
}

// ============================================
// CLOSE ADD TASK MODAL
// ============================================
function closeAddTaskModal() {
    document.getElementById('addTaskModal').style.display = 'none';
    document.getElementById('taskForm').reset();
}

// ============================================
// CREATE NEW TASK
// ============================================
async function createNewTask(event) {
    event.preventDefault();

    const formData = {
        taskName: document.getElementById('taskName').value,
        description: document.getElementById('description').value,
        projectName: document.getElementById('projectName').value,
        moduleName: document.getElementById('moduleName').value,
        assignedTo: document.getElementById('assignedTo').value,
        createdBy: CURRENT_USER,
        startDate: document.getElementById('startDate').value,
        expectedEndDate: document.getElementById('expectedEndDate').value,
        priority: document.getElementById('priority').value,
        deptId: parseInt(document.getElementById('dept').value) || null
    };

    console.log('📤 Sending data:', formData);
    console.log('📤 CURRENT_USER value:', CURRENT_USER);

    try {
        showLoader();

        // FIXED: Changed URL to use API_BASE_URL and correct endpoint
        const response = await fetch(`${API_BASE_URL}/CreateTask`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(formData)
        });

        const result = await response.json();
        console.log('📥 Response status:', response.status);
        console.log('📥 Response data:', result);

        if (response.ok && result.success) {
            showNotification('Task created successfully!', 'success');
            closeAddTaskModal();
            await createTaskCards();
        } else {
            // Extract detailed error messages
            if (result.errors) {
                console.error('❌ Detailed validation errors:');
                for (const [field, messages] of Object.entries(result.errors)) {
                    console.error(`  ${field}:`, messages);
                }

                // Flatten all error messages
                const allErrors = Object.values(result.errors).flat();
                showNotification(allErrors.join(', '), 'error');
            } else {
                showNotification(result.message || result.title || 'Failed to create task', 'error');
            }
        }
    } catch (error) {
        showNotification('Error creating task: ' + error.message, 'error');
    } finally {
        hideLoader();
        location.reload()
    }
}
// ============================================
// HELPER FUNCTIONS
// ============================================
function getStatusClass(status) {
    const statusMap = {
        'NOT_STARTED': 'not-started',
        'IN_PROGRESS': 'in-progress',
        'COMPLETED_AHEAD': 'completed-ahead',
        'COMPLETED_ON_TIME': 'completed-on-time',
        'COMPLETED_LATE': 'completed-late',
        'OVERDUE': 'overdue'
    };
    return statusMap[status] || 'unknown';
}

function getStatusIcon(status) {
    const iconMap = {
        'NOT_STARTED': '<i class="fas fa-circle"></i>',
        'IN_PROGRESS': '<i class="fas fa-spinner fa-spin"></i>',
        'COMPLETED_AHEAD': '<i class="fas fa-check-double"></i>',
        'COMPLETED_ON_TIME': '<i class="fas fa-check-circle"></i>',
        'COMPLETED_LATE': '<i class="fas fa-check"></i>',
        'OVERDUE': '<i class="fas fa-exclamation-triangle"></i>'
    };
    return iconMap[status] || '<i class="fas fa-question"></i>';
}

function formatStatus(status) {
    const statusMap = {
        'NOT_STARTED': 'Not Started',
        'IN_PROGRESS': 'In Progress',
        'COMPLETED_AHEAD': 'Completed Ahead',
        'COMPLETED_ON_TIME': 'Completed On Time',
        'COMPLETED_LATE': 'Completed Late',
        'OVERDUE': 'Overdue'
    };
    return statusMap[status] || status;
}

function formatDate(dateString) {
    if (!dateString || dateString === '0001-01-01T00:00:00') return 'N/A';
    const date = new Date(dateString);
    const options = { year: 'numeric', month: 'short', day: 'numeric' };
    return date.toLocaleDateString('en-US', options);
}

function formatDateTime(dateString) {
    if (!dateString || dateString === '0001-01-01T00:00:00') return 'N/A';
    const date = new Date(dateString);
    const options = {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    };
    return date.toLocaleDateString('en-US', options);
}

function escapeHtml(text) {
    if (!text) return '';
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}

function updateStats() {
    const total = allTask.length;
    const inProgress = allTask.filter(t => t.status === 'IN_PROGRESS').length;
    const completed = allTask.filter(t => t.isCompleted).length;
    const overdue = allTask.filter(t => t.status === 'OVERDUE').length;

    document.getElementById('totalTasks').textContent = total;
    document.getElementById('inProgressTasks').textContent = inProgress;
    document.getElementById('completedTasks').textContent = completed;
    document.getElementById('overdueTasks').textContent = overdue;
}

function showLoader() {
    document.getElementById('loaderOverlay').style.display = 'flex';
}

function hideLoader() {
    document.getElementById('loaderOverlay').style.display = 'none';
}

function showNotification(message, type) {
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.innerHTML = `
        <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i>
        <span>${message}</span>
    `;

    document.body.appendChild(notification);

    setTimeout(() => {
        notification.classList.add('show');
    }, 100);

    setTimeout(() => {
        notification.classList.remove('show');
        setTimeout(() => {
            notification.remove();
        }, 300);
    }, 3000);
}

// Close modal when clicking outside
window.onclick = function (event) {
    const modal = document.getElementById('addTaskModal');
    if (event.target === modal) {
        closeAddTaskModal();
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    createTaskCards();
    calculateCompletePercentage();
})