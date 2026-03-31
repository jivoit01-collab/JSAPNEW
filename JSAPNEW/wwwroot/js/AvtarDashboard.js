/* ========================================
   BUDGET DASHBOARD - JavaScript
   ======================================== */

// ========================================
// STATE MANAGEMENT
// ========================================
let budgetData = [];
let ledgers = [];
let budgets = [];
let selectedBudgets = [];
let selectedLedgers = [];
let selectedMonths = [];
let year = new Date().getFullYear();
let currentYear = year;

let months = generateMonthsList(year);

function generateMonthsList(yr) {
    return [
        { name: `Jan-${yr}`, value: `01-${yr}` },
        { name: `Feb-${yr}`, value: `02-${yr}` },
        { name: `Mar-${yr}`, value: `03-${yr}` },
        { name: `Apr-${yr}`, value: `04-${yr}` },
        { name: `May-${yr}`, value: `05-${yr}` },
        { name: `Jun-${yr}`, value: `06-${yr}` },
        { name: `Jul-${yr}`, value: `07-${yr}` },
        { name: `Aug-${yr}`, value: `08-${yr}` },
        { name: `Sep-${yr}`, value: `09-${yr}` },
        { name: `Oct-${yr}`, value: `10-${yr}` },
        { name: `Nov-${yr}`, value: `11-${yr}` },
        { name: `Dec-${yr}`, value: `12-${yr}` },
    ];
}

// ========================================
// CUSTOM MULTI-SELECT COMPONENT
// ========================================
class MultiSelect {
    constructor(container, options = {}) {
        this.container = container;
        this.options = {
            placeholder: options.placeholder || 'Select items',
            maxVisible: options.maxVisible || 2,
            searchable: options.searchable !== false,
            onChange: options.onChange || (() => { }),
            items: options.items || []
        };
        this.selectedValues = [];
        this.isOpen = false;
        this.init();
    }

    init() {
        this.container.innerHTML = '';
        this.container.classList.add('multi-select-container');

        // Create trigger
        this.trigger = document.createElement('div');
        this.trigger.className = 'multi-select-trigger';
        this.trigger.innerHTML = `
            <span class="placeholder">${this.options.placeholder}</span>
            <svg class="arrow" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3">
                <polyline points="6 9 12 15 18 9"></polyline>
            </svg>
        `;
        this.container.appendChild(this.trigger);

        // Create dropdown
        this.dropdown = document.createElement('div');
        this.dropdown.className = 'multi-select-dropdown';
        this.dropdown.innerHTML = `
            ${this.options.searchable ? `
                <div class="dropdown-search">
                    <input type="text" placeholder="Search..." />
                </div>
            ` : ''}
            <div class="dropdown-select-all">
                <div class="checkbox">
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3">
                        <polyline points="20 6 9 17 4 12"></polyline>
                    </svg>
                </div>
                <span>Select All</span>
            </div>
            <div class="dropdown-options"></div>
            <div class="dropdown-actions">
                <button class="btn-clear" type="button">Clear All</button>
                <button class="btn-done" type="button">Done</button>
            </div>
        `;
        this.container.appendChild(this.dropdown);

        this.optionsContainer = this.dropdown.querySelector('.dropdown-options');
        this.searchInput = this.dropdown.querySelector('.dropdown-search input');
        this.selectAllBtn = this.dropdown.querySelector('.dropdown-select-all');

        this.bindEvents();
        this.renderOptions();
    }

    bindEvents() {
        this.trigger.addEventListener('click', (e) => {
            if (e.target.closest('.remove-tag')) return;
            this.toggle();
        });

        document.addEventListener('click', (e) => {
            if (!this.container.contains(e.target)) {
                this.close();
            }
        });

        if (this.searchInput) {
            this.searchInput.addEventListener('input', (e) => {
                this.filterOptions(e.target.value);
            });
        }

        // Select All functionality
        this.selectAllBtn.addEventListener('click', () => {
            this.toggleSelectAll();
        });

        this.dropdown.querySelector('.btn-clear').addEventListener('click', () => {
            this.clearAll();
        });

        this.dropdown.querySelector('.btn-done').addEventListener('click', () => {
            this.close();
        });
    }

    toggle() {
        this.isOpen ? this.close() : this.open();
    }

    open() {
        this.isOpen = true;
        this.dropdown.classList.add('open');
        this.trigger.classList.add('active');
        if (this.searchInput) {
            this.searchInput.value = '';
            this.filterOptions('');
            setTimeout(() => this.searchInput.focus(), 50);
        }
    }

    close() {
        this.isOpen = false;
        this.dropdown.classList.remove('open');
        this.trigger.classList.remove('active');
    }

    setItems(items) {
        this.options.items = items;
        this.selectedValues = [];
        this.renderOptions();
        this.updateTrigger();
        this.updateSelectAllState();
    }

    renderOptions() {
        this.optionsContainer.innerHTML = '';

        if (this.options.items.length === 0) {
            this.optionsContainer.innerHTML = '<div style="padding: 20px; text-align: center; color: #718096;">No items available</div>';
            return;
        }

        this.options.items.forEach(item => {
            const option = document.createElement('div');
            option.className = 'dropdown-option';
            option.dataset.value = item.value;

            if (this.selectedValues.includes(item.value)) {
                option.classList.add('selected');
            }

            option.innerHTML = `
                <div class="checkbox">
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3">
                        <polyline points="20 6 9 17 4 12"></polyline>
                    </svg>
                </div>
                <span class="option-text" title="${item.label}">${item.label}</span>
            `;

            option.addEventListener('click', () => this.toggleOption(item.value));
            this.optionsContainer.appendChild(option);
        });

        this.updateSelectAllState();
    }

    toggleOption(value) {
        const index = this.selectedValues.indexOf(value);
        if (index > -1) {
            this.selectedValues.splice(index, 1);
        } else {
            this.selectedValues.push(value);
        }

        const option = this.optionsContainer.querySelector(`[data-value="${value}"]`);
        if (option) {
            option.classList.toggle('selected', this.selectedValues.includes(value));
        }

        this.updateTrigger();
        this.updateSelectAllState();
        this.options.onChange(this.selectedValues);
    }

    toggleSelectAll() {
        const allSelected = this.selectedValues.length === this.options.items.length;

        if (allSelected) {
            this.selectedValues = [];
        } else {
            this.selectedValues = this.options.items.map(item => item.value);
        }

        this.optionsContainer.querySelectorAll('.dropdown-option').forEach(opt => {
            const value = opt.dataset.value;
            opt.classList.toggle('selected', this.selectedValues.includes(value));
        });

        this.updateTrigger();
        this.updateSelectAllState();
        this.options.onChange(this.selectedValues);
    }

    updateSelectAllState() {
        if (!this.selectAllBtn) return;

        const allSelected = this.options.items.length > 0 &&
            this.selectedValues.length === this.options.items.length;

        this.selectAllBtn.classList.toggle('all-selected', allSelected);

        const label = this.selectAllBtn.querySelector('span');
        if (label) {
            label.textContent = allSelected ? 'Deselect All' : 'Select All';
        }
    }

    updateTrigger() {
        this.trigger.innerHTML = '';

        if (this.selectedValues.length === 0) {
            const newPlaceholder = document.createElement('span');
            newPlaceholder.className = 'placeholder';
            newPlaceholder.textContent = this.options.placeholder;
            this.trigger.appendChild(newPlaceholder);
        } else {
            const visibleItems = this.selectedValues.slice(0, this.options.maxVisible);

            visibleItems.forEach(value => {
                const item = this.options.items.find(i => i.value === value);
                if (item) {
                    const tag = document.createElement('div');
                    tag.className = 'selected-tag';
                    tag.innerHTML = `
                        <span title="${item.label}">${item.label}</span>
                        <button class="remove-tag" type="button" data-value="${value}">&times;</button>
                    `;
                    tag.querySelector('.remove-tag').addEventListener('click', (e) => {
                        e.stopPropagation();
                        this.toggleOption(value);
                    });
                    this.trigger.appendChild(tag);
                }
            });

            const remaining = this.selectedValues.length - this.options.maxVisible;
            if (remaining > 0) {
                const moreTag = document.createElement('div');
                moreTag.className = 'more-tag';
                moreTag.textContent = `+${remaining} more`;
                this.trigger.appendChild(moreTag);
            }
        }

        const newArrow = document.createElement('svg');
        newArrow.className = 'arrow';
        newArrow.setAttribute('width', '12');
        newArrow.setAttribute('height', '12');
        newArrow.setAttribute('viewBox', '0 0 24 24');
        newArrow.setAttribute('fill', 'none');
        newArrow.setAttribute('stroke', 'currentColor');
        newArrow.setAttribute('stroke-width', '3');
        newArrow.innerHTML = '<polyline points="6 9 12 15 18 9"></polyline>';
        this.trigger.appendChild(newArrow);

        if (this.isOpen) {
            this.trigger.classList.add('active');
        }
    }

    filterOptions(query) {
        const normalizedQuery = query.toLowerCase().trim();
        const options = this.optionsContainer.querySelectorAll('.dropdown-option');

        options.forEach(option => {
            const text = option.querySelector('.option-text').textContent.toLowerCase();
            const matches = normalizedQuery === '' || text.includes(normalizedQuery);
            option.style.display = matches ? '' : 'none';
        });
    }

    clearAll() {
        this.selectedValues = [];
        this.optionsContainer.querySelectorAll('.dropdown-option').forEach(opt => {
            opt.classList.remove('selected');
        });
        this.updateTrigger();
        this.updateSelectAllState();
        this.options.onChange(this.selectedValues);
    }

    getSelected() {
        return [...this.selectedValues];
    }

    setSelected(values) {
        this.selectedValues = [...values];
        this.renderOptions();
        this.updateTrigger();
    }
}

// Store multi-select instances
let ledgerMultiSelect = null;
let budgetMultiSelect = null;
let monthMultiSelect = null;

// ========================================
// LOADER FUNCTIONS
// ========================================
function showLoader() {
    document.getElementById("loaderOverlay").style.display = "flex";
}

function hideLoader() {
    document.getElementById("loaderOverlay").style.display = "none";
}

// ========================================
// DATE DISPLAY
// ========================================
function updateCurrentDate() {
    const dateEl = document.getElementById("current-date");
    if (dateEl) {
        const options = { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' };
        dateEl.textContent = new Date().toLocaleDateString('en-US', options);
    }
}

// ========================================
// BRANCH CHANGE HANDLER
// ========================================
async function branchChange() {
    const filterGroups = ['ledger-filter', 'budget-filter', 'month-filter'];
    const branch = document.getElementById("company-selector").value;

    filterGroups.forEach((id, index) => {
        const el = document.getElementById(id);
        setTimeout(() => {
            el.classList.remove("disabled");
        }, index * 100);
    });

    await populateLedgers(branch);
    await populateBudgets(branch);
    populateMonths();
    await getBudgetData(branch);
    setupTable2(branch);
}

// ========================================
// API FUNCTIONS
// ========================================
async function getLedgers(branch) {
    ledgers = [];
    const url = branch === ''
        ? `/api/Dashboard/GetUniqueAccounts`
        : `/api/Dashboard/GetUniqueAccounts?branch=${branch}`;

    try {
        const response = await fetch(url);
        if (!response.ok) return;
        const result = await response.json();
        result.forEach(acct => ledgers.push(acct.acctName));
    } catch (err) {
        console.error("Error fetching ledgers:", err);
    }
}

async function getBudgets(branch) {
    budgets = [];
    const url = branch === ''
        ? `/api/Dashboard/GetUniqueBudgets`
        : `/api/Dashboard/GetUniqueBudgets?branch=${branch}`;

    try {
        const response = await fetch(url);
        if (!response.ok) return;
        const result = await response.json();
        result.forEach(budget => budgets.push(budget.budget));
    } catch (err) {
        console.error("Error fetching budgets:", err);
    }
}

async function getBudgetData(branch) {
    showLoader();
    const url = branch === ''
        ? `/api/Dashboard/getBudgetDataByBranch`
        : `/api/Dashboard/getBudgetDataByBranch?branch=${branch}`;
    budgetData = [];

    try {
        const response = await fetch(url);
        if (!response.ok) return;
        budgetData = await response.json();
        filterByCompany();
    } catch (err) {
        console.error("Error fetching budget data:", err);
    } finally {
        hideLoader();
    }
}

// ========================================
// POPULATE FUNCTIONS WITH MULTI-SELECT
// ========================================
async function populateLedgers(branch) {
    await getLedgers(branch);

    const filterGroup = document.getElementById("ledger-filter");
    let container = document.getElementById("ledger-multiselect");

    if (!container) {
        container = document.createElement('div');
        container.id = 'ledger-multiselect';
        container.className = 'multi-select-container';

        const oldSelect = document.getElementById("ledger-selector");
        if (oldSelect && oldSelect.parentNode) {
            oldSelect.parentNode.replaceChild(container, oldSelect);
        } else {
            filterGroup.appendChild(container);
        }
    }

    const countEl = document.getElementById("ledger-count");

    ledgerMultiSelect = new MultiSelect(container, {
        placeholder: 'Select Ledgers',
        maxVisible: 2,
        searchable: true,
        items: ledgers.map(l => ({ value: l, label: l })),
        onChange: (values) => {
            selectedLedgers = values;
            if (countEl) countEl.textContent = `${values.length} selected`;
        }
    });
}

async function populateBudgets(branch) {
    await getBudgets(branch);

    const filterGroup = document.getElementById("budget-filter");
    let container = document.getElementById("budget-multiselect");

    if (!container) {
        container = document.createElement('div');
        container.id = 'budget-multiselect';
        container.className = 'multi-select-container';

        const oldSelect = document.getElementById("budget-selector");
        if (oldSelect && oldSelect.parentNode) {
            oldSelect.parentNode.replaceChild(container, oldSelect);
        } else {
            filterGroup.appendChild(container);
        }
    }

    const countEl = document.getElementById("budget-count");

    budgetMultiSelect = new MultiSelect(container, {
        placeholder: 'Select Budgets',
        maxVisible: 2,
        searchable: true,
        items: budgets.map(b => ({ value: b, label: b })),
        onChange: (values) => {
            selectedBudgets = values;
            if (countEl) countEl.textContent = `${values.length} selected`;
        }
    });
}

function populateMonths() {
    const filterGroup = document.getElementById("month-filter");
    let container = document.getElementById("month-multiselect");

    if (!container) {
        container = document.createElement('div');
        container.id = 'month-multiselect';
        container.className = 'multi-select-container';

        const oldSelect = document.getElementById("month-selector");
        if (oldSelect && oldSelect.parentNode) {
            oldSelect.parentNode.replaceChild(container, oldSelect);
        } else {
            filterGroup.appendChild(container);
        }
    }

    const countEl = document.getElementById("month-count");

    monthMultiSelect = new MultiSelect(container, {
        placeholder: 'Select Months',
        maxVisible: 2,
        searchable: false,
        items: months.map(m => ({ value: m.value, label: m.name })),
        onChange: (values) => {
            selectedMonths = values;
            if (countEl) countEl.textContent = `${values.length} selected`;
        }
    });
}

// ========================================
// MAIN FILTER FUNCTION
// ========================================
async function filterByCompany() {
    const branch = document.getElementById("company-selector").value;
    const activeBudgets = selectedBudgets.length > 0 ? [...selectedBudgets] : [...budgets];
    const activeLedgers = selectedLedgers.length > 0 ? [...selectedLedgers] : [...ledgers];
    const activeMonths = selectedMonths.length > 0 ? [...selectedMonths] : [];

    let filtered = [...budgetData];

    if (branch && branch !== "ALL" && branch !== "") {
        filtered = filtered.filter(r => r.branch === branch);
    }
    filtered = filtered.filter(r => activeBudgets.includes(r.budget));
    filtered = filtered.filter(r => activeLedgers.includes(r.acctName));
    if (activeMonths.length > 0) {
        filtered = filtered.filter(r => activeMonths.includes(r.currentMonth));
    }

    const container = document.getElementById('table-container-1');
    const emptyState = document.getElementById('empty-state-1');

    if (filtered.length === 0) {
        if (container) container.style.display = 'none';
        if (emptyState) emptyState.style.display = 'flex';
        return;
    } else {
        if (container) container.style.display = 'block';
        if (emptyState) emptyState.style.display = 'none';
    }

    const multipleBudgets = activeBudgets.length > 1;
    const multipleMonths = activeMonths.length > 1;

    if (multipleBudgets && multipleMonths) {
        createHorizontalBudgetMonthTable(filtered, activeBudgets, activeLedgers, activeMonths);
    } else if (multipleMonths) {
        createLedgerMonthTable(filtered, activeLedgers, activeMonths);
    } else if (multipleBudgets) {
        createLedgerBudgetTable(filtered, activeBudgets, activeLedgers, activeMonths);
    } else {
        createSimpleTable(filtered, activeLedgers, activeMonths);
    }
}

// ========================================
// TABLE CREATION FUNCTIONS
// ========================================
function formatCurrency(amount) {
    if (amount === 0 || amount === null || amount === undefined) return "—";
    return amount.toLocaleString("en-IN", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });
}

function createHorizontalBudgetMonthTable(filtered, activeBudgets, activeLedgers, activeMonths) {
    const tableElement = document.getElementById("master-table");
    tableElement.innerHTML = "";

    const thead = document.createElement("thead");
    const totalDataColumns = activeBudgets.length * activeMonths.length;
    const showDifference = totalDataColumns === 2;

    const budgetHeaderRow = document.createElement("tr");

    const cornerCell = document.createElement("th");
    cornerCell.textContent = "Ledger Account";
    cornerCell.rowSpan = 2;
    cornerCell.style.textAlign = "left";
    cornerCell.style.minWidth = "220px";
    cornerCell.style.verticalAlign = "middle";
    budgetHeaderRow.appendChild(cornerCell);

    activeBudgets.forEach(budget => {
        const th = document.createElement("th");
        th.textContent = budget;
        th.colSpan = activeMonths.length;
        th.style.textAlign = "center";
        budgetHeaderRow.appendChild(th);
    });

    if (showDifference) {
        const diffHeader = document.createElement("th");
        diffHeader.textContent = "Variance";
        diffHeader.rowSpan = 2;
        diffHeader.style.verticalAlign = "middle";
        diffHeader.style.background = "#f59f00";
        diffHeader.style.color = "white";
        budgetHeaderRow.appendChild(diffHeader);
    }

    const totalHeader = document.createElement("th");
    totalHeader.textContent = "Total";
    totalHeader.rowSpan = 2;
    totalHeader.style.verticalAlign = "middle";
    totalHeader.style.background = "#40c057";
    totalHeader.style.color = "white";
    budgetHeaderRow.appendChild(totalHeader);

    thead.appendChild(budgetHeaderRow);

    const monthHeaderRow = document.createElement("tr");
    activeBudgets.forEach(budget => {
        activeMonths.forEach(month => {
            const th = document.createElement("th");
            th.textContent = formatMonth(month);
            th.style.cursor = "pointer";
            th.style.fontSize = "11px";
            th.dataset.budget = budget;
            th.dataset.month = month;
            th.addEventListener("click", () => sortByBudgetMonth(tableElement, budget, month));
            monthHeaderRow.appendChild(th);
        });
    });
    thead.appendChild(monthHeaderRow);
    tableElement.appendChild(thead);

    const tbody = document.createElement("tbody");
    let columnTotals = {};
    activeBudgets.forEach(budget => {
        columnTotals[budget] = Array(activeMonths.length).fill(0);
    });
    let grandTotalSum = 0;
    let differenceTotal = 0;

    activeLedgers.forEach(ledger => {
        const tr = document.createElement("tr");

        const ledgerCell = document.createElement("td");
        ledgerCell.textContent = ledger;
        tr.appendChild(ledgerCell);

        let rowTotal = 0;
        let rowValues = [];

        activeBudgets.forEach(budget => {
            activeMonths.forEach((month, monthIdx) => {
                const amount = filtered
                    .filter(r => r.acctName === ledger && r.budget === budget && r.currentMonth === month)
                    .reduce((sum, r) => sum + (r.amount || 0), 0);

                const td = document.createElement("td");
                td.textContent = formatCurrency(amount);
                td.dataset.value = amount;
                tr.appendChild(td);

                rowTotal += amount;
                rowValues.push(amount);
                columnTotals[budget][monthIdx] += amount;
            });
        });

        if (showDifference && rowValues.length === 2) {
            const difference = rowValues[1] - rowValues[0];
            const diffCell = document.createElement("td");
            diffCell.textContent = formatCurrency(difference);
            diffCell.style.fontWeight = "600";
            diffCell.style.backgroundColor = difference >= 0 ? "rgba(64, 192, 87, 0.1)" : "rgba(250, 82, 82, 0.1)";
            diffCell.style.color = difference >= 0 ? "#40c057" : "#fa5252";
            diffCell.dataset.value = difference;
            tr.appendChild(diffCell);
            differenceTotal += difference;
        }

        const totalCell = document.createElement("td");
        totalCell.textContent = formatCurrency(rowTotal);
        totalCell.style.fontWeight = "700";
        totalCell.style.backgroundColor = "rgba(64, 192, 87, 0.05)";
        totalCell.dataset.value = rowTotal;
        tr.appendChild(totalCell);

        grandTotalSum += rowTotal;
        tbody.appendChild(tr);
    });

    const totalRow = document.createElement("tr");
    totalRow.className = "total-row";

    const totalLabel = document.createElement("td");
    totalLabel.textContent = "Total";
    totalRow.appendChild(totalLabel);

    activeBudgets.forEach(budget => {
        columnTotals[budget].forEach(total => {
            const td = document.createElement("td");
            td.textContent = formatCurrency(total);
            totalRow.appendChild(td);
        });
    });

    if (showDifference) {
        const diffTotalCell = document.createElement("td");
        diffTotalCell.textContent = formatCurrency(differenceTotal);
        totalRow.appendChild(diffTotalCell);
    }

    const grandTotalCell = document.createElement("td");
    grandTotalCell.textContent = formatCurrency(grandTotalSum);
    totalRow.appendChild(grandTotalCell);

    tbody.appendChild(totalRow);
    tableElement.appendChild(tbody);
}

function createLedgerMonthTable(filtered, activeLedgers, activeMonths) {
    const tableElement = document.getElementById("master-table");
    tableElement.innerHTML = "";

    const showDifference = activeMonths.length === 2;

    const thead = document.createElement("thead");
    const headerRow = document.createElement("tr");

    const corner = document.createElement("th");
    corner.textContent = "Ledger Account";
    corner.style.textAlign = "left";
    corner.style.minWidth = "220px";
    headerRow.appendChild(corner);

    activeMonths.forEach((month, i) => {
        const th = document.createElement("th");
        th.textContent = formatMonth(month);
        th.style.cursor = "pointer";
        th.dataset.columnIndex = i + 1;
        th.addEventListener("click", () => sortColumn(tableElement, i + 1));
        headerRow.appendChild(th);
    });

    if (showDifference) {
        const diffTh = document.createElement("th");
        diffTh.textContent = "Variance";
        diffTh.style.background = "#f59f00";
        diffTh.style.color = "white";
        headerRow.appendChild(diffTh);
    }

    const totalTh = document.createElement("th");
    totalTh.textContent = "Total";
    totalTh.style.background = "#40c057";
    totalTh.style.color = "white";
    headerRow.appendChild(totalTh);

    thead.appendChild(headerRow);
    tableElement.appendChild(thead);

    const tbody = document.createElement("tbody");
    let columnTotals = Array(activeMonths.length).fill(0);
    let differenceTotal = 0;
    let grandTotal = 0;

    activeLedgers.forEach(ledger => {
        const tr = document.createElement("tr");

        const ledgerCell = document.createElement("td");
        ledgerCell.textContent = ledger;
        tr.appendChild(ledgerCell);

        let rowTotal = 0;
        let rowValues = [];

        activeMonths.forEach((month, idx) => {
            const amount = filtered
                .filter(r => r.acctName === ledger && r.currentMonth === month)
                .reduce((sum, r) => sum + (r.amount || 0), 0);

            const td = document.createElement("td");
            td.textContent = formatCurrency(amount);
            td.dataset.value = amount;
            tr.appendChild(td);

            rowTotal += amount;
            rowValues.push(amount);
            columnTotals[idx] += amount;
        });

        if (showDifference && rowValues.length === 2) {
            const difference = rowValues[1] - rowValues[0];
            const diffCell = document.createElement("td");
            diffCell.textContent = formatCurrency(difference);
            diffCell.style.fontWeight = "600";
            diffCell.style.backgroundColor = difference >= 0 ? "rgba(64, 192, 87, 0.1)" : "rgba(250, 82, 82, 0.1)";
            diffCell.style.color = difference >= 0 ? "#40c057" : "#fa5252";
            diffCell.dataset.value = difference;
            tr.appendChild(diffCell);
            differenceTotal += difference;
        }

        const totalCell = document.createElement("td");
        totalCell.textContent = formatCurrency(rowTotal);
        totalCell.style.fontWeight = "700";
        totalCell.style.backgroundColor = "rgba(64, 192, 87, 0.05)";
        totalCell.dataset.value = rowTotal;
        tr.appendChild(totalCell);

        grandTotal += rowTotal;
        tbody.appendChild(tr);
    });

    const totalRow = document.createElement("tr");
    totalRow.className = "total-row";

    const totalLabel = document.createElement("td");
    totalLabel.textContent = "Total";
    totalRow.appendChild(totalLabel);

    columnTotals.forEach(total => {
        const td = document.createElement("td");
        td.textContent = formatCurrency(total);
        totalRow.appendChild(td);
    });

    if (showDifference) {
        const diffTotalCell = document.createElement("td");
        diffTotalCell.textContent = formatCurrency(differenceTotal);
        totalRow.appendChild(diffTotalCell);
    }

    const grandTotalCell = document.createElement("td");
    grandTotalCell.textContent = formatCurrency(grandTotal);
    totalRow.appendChild(grandTotalCell);

    tbody.appendChild(totalRow);
    tableElement.appendChild(tbody);
}

function createLedgerBudgetTable(filtered, activeBudgets, activeLedgers, activeMonths) {
    const tableElement = document.getElementById("master-table");
    tableElement.innerHTML = "";

    const showDifference = activeBudgets.length === 2;

    const thead = document.createElement("thead");
    const headerRow = document.createElement("tr");

    const corner = document.createElement("th");
    corner.textContent = "Ledger Account";
    corner.style.textAlign = "left";
    corner.style.minWidth = "220px";
    headerRow.appendChild(corner);

    activeBudgets.forEach((budget, i) => {
        const th = document.createElement("th");
        th.textContent = budget;
        th.style.cursor = "pointer";
        th.dataset.columnIndex = i + 1;
        th.addEventListener("click", () => sortColumn(tableElement, i + 1));
        headerRow.appendChild(th);
    });

    if (showDifference) {
        const diffTh = document.createElement("th");
        diffTh.textContent = "Variance";
        diffTh.style.background = "#f59f00";
        diffTh.style.color = "white";
        headerRow.appendChild(diffTh);
    }

    const totalTh = document.createElement("th");
    totalTh.textContent = "Total";
    totalTh.style.background = "#40c057";
    totalTh.style.color = "white";
    headerRow.appendChild(totalTh);

    thead.appendChild(headerRow);
    tableElement.appendChild(thead);

    const tbody = document.createElement("tbody");
    let columnTotals = Array(activeBudgets.length).fill(0);
    let differenceTotal = 0;
    let grandTotal = 0;

    activeLedgers.forEach(ledger => {
        const tr = document.createElement("tr");

        const ledgerCell = document.createElement("td");
        ledgerCell.textContent = ledger;
        tr.appendChild(ledgerCell);

        let rowTotal = 0;
        let rowValues = [];

        activeBudgets.forEach((budget, idx) => {
            const amount = filtered
                .filter(r => r.acctName === ledger && r.budget === budget)
                .reduce((sum, r) => sum + (r.amount || 0), 0);

            const td = document.createElement("td");
            td.textContent = formatCurrency(amount);
            td.dataset.value = amount;
            tr.appendChild(td);

            rowTotal += amount;
            rowValues.push(amount);
            columnTotals[idx] += amount;
        });

        if (showDifference && rowValues.length === 2) {
            const difference = rowValues[1] - rowValues[0];
            const diffCell = document.createElement("td");
            diffCell.textContent = formatCurrency(difference);
            diffCell.style.fontWeight = "600";
            diffCell.style.backgroundColor = difference >= 0 ? "rgba(64, 192, 87, 0.1)" : "rgba(250, 82, 82, 0.1)";
            diffCell.style.color = difference >= 0 ? "#40c057" : "#fa5252";
            diffCell.dataset.value = difference;
            tr.appendChild(diffCell);
            differenceTotal += difference;
        }

        const totalCell = document.createElement("td");
        totalCell.textContent = formatCurrency(rowTotal);
        totalCell.style.fontWeight = "700";
        totalCell.style.backgroundColor = "rgba(64, 192, 87, 0.05)";
        totalCell.dataset.value = rowTotal;
        tr.appendChild(totalCell);

        grandTotal += rowTotal;
        tbody.appendChild(tr);
    });

    const totalRow = document.createElement("tr");
    totalRow.className = "total-row";

    const totalLabel = document.createElement("td");
    totalLabel.textContent = "Total";
    totalRow.appendChild(totalLabel);

    columnTotals.forEach(total => {
        const td = document.createElement("td");
        td.textContent = formatCurrency(total);
        totalRow.appendChild(td);
    });

    if (showDifference) {
        const diffTotalCell = document.createElement("td");
        diffTotalCell.textContent = formatCurrency(differenceTotal);
        totalRow.appendChild(diffTotalCell);
    }

    const grandTotalCell = document.createElement("td");
    grandTotalCell.textContent = formatCurrency(grandTotal);
    totalRow.appendChild(grandTotalCell);

    tbody.appendChild(totalRow);
    tableElement.appendChild(tbody);
}

function createSimpleTable(filtered, activeLedgers, activeMonths) {
    const tableElement = document.getElementById("master-table");
    tableElement.innerHTML = "";

    const thead = document.createElement("thead");
    const headerRow = document.createElement("tr");

    const th1 = document.createElement("th");
    th1.textContent = "Ledger Account";
    th1.style.textAlign = "left";
    th1.style.minWidth = "220px";
    headerRow.appendChild(th1);

    const th2 = document.createElement("th");
    th2.textContent = "Amount";
    th2.style.cursor = "pointer";
    th2.dataset.columnIndex = 1;
    th2.addEventListener("click", () => sortColumn(tableElement, 1));
    headerRow.appendChild(th2);

    thead.appendChild(headerRow);
    tableElement.appendChild(thead);

    const tbody = document.createElement("tbody");
    let grandTotal = 0;

    activeLedgers.forEach(ledger => {
        const amount = filtered
            .filter(r => r.acctName === ledger)
            .reduce((sum, r) => sum + (r.amount || 0), 0);

        if (amount > 0) {
            const tr = document.createElement("tr");

            const ledgerCell = document.createElement("td");
            ledgerCell.textContent = ledger;
            tr.appendChild(ledgerCell);

            const amountCell = document.createElement("td");
            amountCell.textContent = formatCurrency(amount);
            amountCell.dataset.value = amount;
            tr.appendChild(amountCell);

            tbody.appendChild(tr);
            grandTotal += amount;
        }
    });

    const totalRow = document.createElement("tr");
    totalRow.className = "total-row";

    const totalLabel = document.createElement("td");
    totalLabel.textContent = "Total";
    totalRow.appendChild(totalLabel);

    const totalAmount = document.createElement("td");
    totalAmount.textContent = formatCurrency(grandTotal);
    totalRow.appendChild(totalAmount);

    tbody.appendChild(totalRow);
    tableElement.appendChild(tbody);
}

// ========================================
// SORTING FUNCTIONS
// ========================================
function sortColumn(table, columnIndex) {
    const tbody = table.querySelector("tbody");
    const rows = Array.from(tbody.querySelectorAll("tr:not(.total-row)"));

    const currentSort = table.getAttribute("data-sort-col") == columnIndex
        ? table.getAttribute("data-sort-order")
        : null;

    const newSortOrder = currentSort === "asc" ? "desc" : "asc";
    table.setAttribute("data-sort-col", columnIndex);
    table.setAttribute("data-sort-order", newSortOrder);

    rows.sort((a, b) => {
        const cellA = a.children[columnIndex];
        const cellB = b.children[columnIndex];
        const valA = parseFloat(cellA?.dataset?.value || cellA?.textContent?.replace(/,/g, "").trim()) || 0;
        const valB = parseFloat(cellB?.dataset?.value || cellB?.textContent?.replace(/,/g, "").trim()) || 0;
        return newSortOrder === "asc" ? valA - valB : valB - valA;
    });

    rows.forEach(row => tbody.appendChild(row));
    const totalRow = tbody.querySelector(".total-row");
    if (totalRow) tbody.appendChild(totalRow);
}

function sortByBudgetMonth(table, budget, month) {
    const tbody = table.querySelector("tbody");
    const rows = Array.from(tbody.querySelectorAll("tr:not(.total-row)"));
    const headers = Array.from(table.querySelectorAll("thead tr:nth-child(2) th"));

    let columnIndex = -1;
    headers.forEach((th, idx) => {
        if (th.dataset.budget === budget && th.dataset.month === month) {
            columnIndex = idx + 1;
        }
    });

    if (columnIndex === -1) return;

    const currentSort = table.getAttribute("data-sort-col") == columnIndex
        ? table.getAttribute("data-sort-order")
        : null;

    const newSortOrder = currentSort === "asc" ? "desc" : "asc";
    table.setAttribute("data-sort-col", columnIndex);
    table.setAttribute("data-sort-order", newSortOrder);

    rows.sort((a, b) => {
        const cellA = a.children[columnIndex];
        const cellB = b.children[columnIndex];
        const valA = parseFloat(cellA?.dataset?.value || cellA?.textContent?.replace(/,/g, "").trim()) || 0;
        const valB = parseFloat(cellB?.dataset?.value || cellB?.textContent?.replace(/,/g, "").trim()) || 0;
        return newSortOrder === "asc" ? valA - valB : valB - valA;
    });

    rows.forEach(row => tbody.appendChild(row));
    const totalRow = tbody.querySelector(".total-row");
    if (totalRow) tbody.appendChild(totalRow);
}

// ========================================
// RESET FUNCTION
// ========================================
function resetFilters() {
    selectedBudgets = [];
    selectedLedgers = [];
    selectedMonths = [];

    if (ledgerMultiSelect) ledgerMultiSelect.clearAll();
    if (budgetMultiSelect) budgetMultiSelect.clearAll();
    if (monthMultiSelect) monthMultiSelect.clearAll();

    document.getElementById("ledger-count").textContent = "0 selected";
    document.getElementById("budget-count").textContent = "0 selected";
    document.getElementById("month-count").textContent = "0 selected";

    document.getElementById("master-table").innerHTML = "";

    const container = document.getElementById('table-container-1');
    const emptyState = document.getElementById('empty-state-1');
    if (container) container.style.display = 'none';
    if (emptyState) emptyState.style.display = 'flex';
}

// ========================================
// SECONDARY TABLE FUNCTIONS
// ========================================
function setupMonths(monthsList) {
    const monthSelector = document.querySelector('.month2-selector');
    monthSelector.innerHTML = '';

    monthsList.forEach(month => {
        const div = document.createElement('div');
        div.innerHTML = `
            <input type="checkbox"
                id="month-${month.value}"
                name="months"
                value="${month.value}"
                class="month2-name"
                onchange="filterByMonths()"/>
            <label for="month-${month.value}">${month.name}</label>
        `;
        monthSelector.appendChild(div);
    });
}

function getFullYear(start, end) {
    const yearSelector = document.querySelector(".year-selector");
    yearSelector.innerHTML = '';

    for (let i = end; i >= start; i--) {
        const option = document.createElement("option");
        option.textContent = i;
        option.value = i;
        if (i === currentYear) option.selected = true;
        yearSelector.appendChild(option);
    }
}

async function setupBudgetFilter(branch) {
    await getBudgets(branch);

    const budgetSelector = document.querySelector('.budget2-selector');
    budgetSelector.innerHTML = '';

    budgets.forEach(budget => {
        const div = document.createElement('div');
        div.innerHTML = `
            <input type="checkbox"
                id="budget2-${budget.replace(/\s+/g, '-')}"
                name="${budget}"
                value="${budget}"
                class="budget2-name"
                onchange="filterByMonths()"/>
            <label for="budget2-${budget.replace(/\s+/g, '-')}">${budget}</label>
        `;
        budgetSelector.appendChild(div);
    });
}

function setYear() {
    const selectedYear = document.querySelector('.year-selector');
    year = selectedYear.value;
    months = generateMonthsList(year);
    setupMonths(months);
    filterByMonths();
}

function setupTable2(branch) {
    setupBudgetFilter(branch);
    getFullYear(2020, 2025);
    setYear();
}

async function filterByMonths() {
    const branch = document.getElementById("company-selector").value;
    const table = document.getElementById('master-table2');
    const budgetChecks = document.querySelectorAll('.budget2-name');
    const monthChecks = document.querySelectorAll('.month2-name');

    let filteredData = branch && branch !== 'ALL'
        ? budgetData.filter(item => item.branch === branch)
        : [...budgetData];

    const activeBudgets = Array.from(budgetChecks).filter(b => b.checked).map(b => b.value);
    const activeMonths = Array.from(monthChecks).filter(m => m.checked).map(m => m.value);

    if (activeBudgets.length > 0) {
        filteredData = filteredData.filter(item => activeBudgets.includes(item.budget));
    }
    if (activeMonths.length > 0) {
        filteredData = filteredData.filter(item => activeMonths.includes(item.currentMonth));
    }

    createPivotTable(filteredData, table);
}

function createPivotTable(data, tableElement) {
    const monthsSet = [...new Set(data.map(item => item.currentMonth))].sort();
    const budgetsList = [...new Set(data.map(item => item.budget))].sort();

    if (monthsSet.length === 0 || budgetsList.length === 0) {
        tableElement.innerHTML = '<tbody><tr><td style="text-align: center; padding: 40px; color: #718096;">Select months and budgets to view data</td></tr></tbody>';
        return;
    }

    const pivot = {};
    budgetsList.forEach(budget => {
        pivot[budget] = {};
        monthsSet.forEach(month => pivot[budget][month] = 0);
    });

    data.forEach(item => {
        if (pivot[item.budget] && item.currentMonth) {
            pivot[item.budget][item.currentMonth] += (item.amount || 0);
        }
    });

    tableElement.innerHTML = '';

    const thead = document.createElement('thead');
    const headerRow = document.createElement('tr');

    const cornerTh = document.createElement('th');
    cornerTh.textContent = 'Budget';
    cornerTh.style.textAlign = 'left';
    headerRow.appendChild(cornerTh);

    monthsSet.forEach(month => {
        const th = document.createElement('th');
        th.textContent = formatMonth(month);
        headerRow.appendChild(th);
    });

    const totalTh = document.createElement('th');
    totalTh.textContent = 'Total';
    totalTh.style.background = '#40c057';
    totalTh.style.color = 'white';
    headerRow.appendChild(totalTh);

    thead.appendChild(headerRow);
    tableElement.appendChild(thead);

    const tbody = document.createElement('tbody');
    const columnTotals = monthsSet.map(() => 0);
    let grandTotal = 0;

    budgetsList.forEach(budget => {
        const tr = document.createElement('tr');

        const budgetCell = document.createElement('td');
        budgetCell.textContent = budget;
        tr.appendChild(budgetCell);

        let rowTotal = 0;
        monthsSet.forEach((month, idx) => {
            const value = pivot[budget][month];
            const td = document.createElement('td');
            td.textContent = formatCurrency(value);
            td.dataset.value = value;
            tr.appendChild(td);
            rowTotal += value;
            columnTotals[idx] += value;
        });

        const totalCell = document.createElement('td');
        totalCell.textContent = formatCurrency(rowTotal);
        totalCell.style.fontWeight = '700';
        totalCell.style.backgroundColor = 'rgba(64, 192, 87, 0.05)';
        tr.appendChild(totalCell);

        grandTotal += rowTotal;
        tbody.appendChild(tr);
    });

    const totalRow = document.createElement('tr');
    totalRow.className = 'total-row';

    const totalLabel = document.createElement('td');
    totalLabel.textContent = 'Total';
    totalRow.appendChild(totalLabel);

    columnTotals.forEach(total => {
        const td = document.createElement('td');
        td.textContent = formatCurrency(total);
        totalRow.appendChild(td);
    });

    const grandTotalCell = document.createElement('td');
    grandTotalCell.textContent = formatCurrency(grandTotal);
    totalRow.appendChild(grandTotalCell);

    tbody.appendChild(totalRow);
    tableElement.appendChild(tbody);
}

// ========================================
// UTILITY FUNCTIONS
// ========================================
function formatMonth(monthValue) {
    const monthNames = {
        '01': 'Jan', '02': 'Feb', '03': 'Mar', '04': 'Apr',
        '05': 'May', '06': 'Jun', '07': 'Jul', '08': 'Aug',
        '09': 'Sep', '10': 'Oct', '11': 'Nov', '12': 'Dec'
    };
    const [month, yr] = monthValue.split('-');
    return `${monthNames[month]}-${yr}`;
}

// ========================================
// INITIALIZATION
// ========================================
document.addEventListener("DOMContentLoaded", () => {
    console.log("Budget Dashboard Initialized");
    updateCurrentDate();
});