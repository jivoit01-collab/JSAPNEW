//Variables 
const months = [
    { month: 'Jan', value: '01' },
    { month: 'Feb', value: '02' },
    { month: 'Mar', value: '03' },
    { month: 'Apr', value: '04' },
    { month: 'May', value: '05' },
    { month: 'Jun', value: '06' },
    { month: 'Jul', value: '07' },
    { month: 'Aug', value: '08' },
    { month: 'Sep', value: '09' },
    { month: 'Oct', value: '10' },
    { month: 'Nov', value: '11' },
    { month: 'Dec', value: '12' },
]
const types = [
    { type: 'Total', value: 'total' },
    { type: 'Approved', value: 'totalApproved' },
    { type: 'Pending', value: 'totalPending' },
    { type: 'Rejected', value: 'totalRejected' },
]

let globalData = [];
let cachedData = [];
let tab2Data = [];

let approved = [];
let rejected = [];
let pending = [];
let flowData = [];

// Pagination state
let currentPage = 1;
let pageSize = 10;
let totalRecords = 0;
let currentDisplayData = [];

//Utility Function 
function showLoader() {
    document.getElementById("loaderOverlay").style.display = "flex";
}

function hideLoader() {
    document.getElementById("loaderOverlay").style.display = "none";
}

function getDate() {
    const month = document.getElementById('month').value
    const year = document.getElementById('year').value
    return month + '-' + year;
}

function getType() {
    const type = document.getElementById('type').value
    return type;
}

//Dynamic year list Initialization
function getYearsInRange(startYear, endYear) {
    const years = []
    for (let year = startYear; year <= endYear; year++) {
        years.push(year);
    }
    return years;
}

//Month Dropdown Setup
function setupMonths() {
    const monthDropdown = document.getElementById('month')
    months.forEach(month => {
        const option = document.createElement('option')
        option.value = month.value
        option.textContent = month.month
        monthDropdown.appendChild(option)
    })
}

function setupYears() {
    const yearDropDown = document.getElementById('year')
    const currentYear = new Date().getFullYear();
    const years = getYearsInRange(2020, currentYear);

    years.forEach(year => {
        const option = document.createElement('option')
        option.value = year
        option.textContent = year
        yearDropDown.appendChild(option)
    })
}

function setupTypes() {
    const typeDropdown = document.getElementById('type')
    types.forEach(type => {
        const option = document.createElement('option')
        option.value = type.value
        option.textContent = type.type
        typeDropdown.appendChild(option)
    })
}

//Setting Default Values
async function setDefaultValues() {
    console.log('Setting Default Values...')
    const monthDropdown = document.getElementById('month')
    const yearDropdown = document.getElementById('year')
    const typeDropdown = document.getElementById('type')

    const currentMonth = new Date().getMonth() + 1
    monthDropdown.value = String(currentMonth).padStart(2, '0')

    const currentYear = new Date().getFullYear()
    yearDropdown.value = currentYear

    typeDropdown.value = 'total'

    await filterData()
}

async function setDefaultValuesTab2() {
    console.log('Setting Default Values for Tab 2...')
    const monthDropdown = document.getElementById('month-tab2')
    const yearDropdown = document.getElementById('year-tab2')

    const currentMonth = new Date().getMonth() + 1
    monthDropdown.value = String(currentMonth).padStart(2, '0')

    const currentYear = new Date().getFullYear()
    yearDropdown.value = currentYear
}

// Setup functions for Tab 2
function setupMonthsTab2() {
    const monthDropdown = document.getElementById('month-tab2')
    months.forEach(month => {
        const option = document.createElement('option')
        option.value = month.value
        option.textContent = month.month
        monthDropdown.appendChild(option)
    })
}

function setupYearsTab2() {
    const yearDropDown = document.getElementById('year-tab2')
    const currentYear = new Date().getFullYear();
    const years = getYearsInRange(2020, currentYear);

    years.forEach(year => {
        const option = document.createElement('option')
        option.value = year
        option.textContent = year
        yearDropDown.appendChild(option)
    })
}

// Utility functions for Tab 2
function getDateTab2() {
    const month = document.getElementById('month-tab2').value
    const year = document.getElementById('year-tab2').value
    return month + '-' + year;
}

//Main API Function : for Fetching with Caching
async function getBudgetData(date) {
    // Check if data is already cached
    if (cachedData[date]) {
        console.log(`Using cached data for ${date}`);
        globalData = cachedData[date];
        return cachedData[date];
    }

    showLoader();
    if (date) {
        try {
            const url = `/api/auth/GetAllBudgetInsight?company=${companyId}&month=${date}`
            const response = await fetch(url)

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json()
            let budgetData = data.data

            budgetData = budgetData.filter(budget => budget.type === 'Budget')

            // Cache the data
            cachedData[date] = budgetData;
            globalData = budgetData;
            console.log(`Data cached for ${date}`);
            return budgetData;

        } catch (error) {
            console.error('Error Fetching Data', error);
            return [];
        } finally {
            hideLoader();
        }
    }
}

async function filterData() {
    const date = getDate();
    const type = getType();

    // Only fetch if we don't have cached data
    if (!cachedData[date]) {
        await getBudgetData(date);
    } else {
        // Use cached data
        globalData = cachedData[date];
    }

    currentPage = 1; // Reset to first page when filtering
    let budgetData = [...globalData]; // Create a copy
    const finalCell = document.getElementById('final-cell')

    console.log(`Filtering data by ${type}`)

    if (type === 'total') {
        budgetData = budgetData.map(budget => ({
            ...budget,
            count: budget.totalPending + budget.totalApproved + budget.totalRejected
        }))
        currentDisplayData = budgetData
        totalRecords = budgetData.length
        finalCell.innerHTML = 'Total'
        showFullTable(getPaginatedData(budgetData))
    } else if (type === 'totalPending') {
        budgetData = budgetData.map(budget => ({
            ...budget,
            count: budget.totalPending
        }))
        finalCell.innerHTML = 'Total Pending'
        currentDisplayData = budgetData
        totalRecords = budgetData.length
        showTable(getPaginatedData(budgetData));
    } else if (type === 'totalApproved') {
        budgetData = budgetData.map(budget => ({
            ...budget,
            count: budget.totalApproved
        }))
        finalCell.innerHTML = 'Total Approved'
        currentDisplayData = budgetData
        totalRecords = budgetData.length
        showTable(getPaginatedData(budgetData));
    } else if (type === 'totalRejected') {
        budgetData = budgetData.map(budget => ({
            ...budget,
            count: budget.totalRejected
        }))
        finalCell.innerHTML = 'Total Rejected'
        currentDisplayData = budgetData
        totalRecords = budgetData.length
        showTable(getPaginatedData(budgetData));
    }

    updatePaginationControls();
}

function showTable(budgetData) {
    const table = document.getElementById('budget-data-table-body');
    const hiddenCell = document.querySelectorAll('.hidden-cell')

    if (hiddenCell.length > 0 && hiddenCell[0].style.display !== 'none') {
        hiddenCell.forEach(cell => cell.style.display = 'none')
    }

    if (!table) return;
    table.innerHTML = '';

    if (budgetData.length === 0) {
        table.innerHTML = '<tr><td colspan="3" style="text-align: center; padding: 30px; color: #64748b;">No records found</td></tr>';
        return;
    }

    budgetData.forEach(budget => {
        const tr = document.createElement('tr')
        tr.innerHTML = `
            <td>${budget.userID}</td>
            <td>${budget.userName}</td>
            <td><strong>${budget.count}</strong></td>
        `
        table.appendChild(tr);
    })
}

function showFullTable(budgetData) {
    const table = document.getElementById('budget-data-table-body');
    const hiddenCell = document.querySelectorAll('.hidden-cell')

    if (hiddenCell.length > 0 && hiddenCell[0].style.display !== 'table-cell') {
        hiddenCell.forEach(cell => cell.style.display = 'table-cell')
    }

    if (!table) return;
    table.innerHTML = '';

    if (budgetData.length === 0) {
        table.innerHTML = '<tr><td colspan="6" style="text-align: center; padding: 30px; color: #64748b;">No records found</td></tr>';
        return;
    }

    budgetData.forEach(budget => {
        const tr = document.createElement('tr')
        tr.onclick = () => { openModal(budget.userID, budget.userName) }
        tr.innerHTML = `
            <td>${budget.userID}</td>
            <td>${budget.userName}</td>
            <td>${budget.totalPending}</td>
            <td>${budget.totalApproved}</td>
            <td>${budget.totalRejected}</td>
            <td><strong>${budget.count}</strong></td>
        `
        table.appendChild(tr);
    })
}



function setupSearch() {
    const searchInput = document.getElementById('search');

    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            searchData(e.target.value.toLowerCase());
        });
    }
}














function searchData(searchTerm = '') {
    currentPage = 1; // Reset to first page when searching
    let budgetData = [...globalData]; // Use cached global data
    const type = getType();

    console.log(`Searching data with term: ${searchTerm}`)

    if (searchTerm) {
        budgetData = budgetData.filter(budget =>
            budget.userName.toLowerCase().includes(searchTerm) ||
            budget.userID.toString().includes(searchTerm)
        );
    }

    if (type === 'total') {
        budgetData = budgetData.map(budget => ({
            ...budget,
            count: budget.totalPending + budget.totalApproved + budget.totalRejected
        }))
        currentDisplayData = budgetData
        totalRecords = budgetData.length
        showFullTable(getPaginatedData(budgetData))
    } else if (type === 'totalPending') {
        budgetData = budgetData.map(budget => ({
            ...budget,
            count: budget.totalPending
        }))
        currentDisplayData = budgetData
        totalRecords = budgetData.length
        showTable(getPaginatedData(budgetData));
    } else if (type === 'totalApproved') {
        budgetData = budgetData.map(budget => ({
            ...budget,
            count: budget.totalApproved
        }))
        currentDisplayData = budgetData
        totalRecords = budgetData.length
        showTable(getPaginatedData(budgetData));
    } else if (type === 'totalRejected') {
        budgetData = budgetData.map(budget => ({
            ...budget,
            count: budget.totalRejected
        }))
        currentDisplayData = budgetData
        totalRecords = budgetData.length
        showTable(getPaginatedData(budgetData));
    }

    updatePaginationControls();
}

// Pagination Functions
function getPaginatedData(data) {
    const startIndex = (currentPage - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    return data.slice(startIndex, endIndex);
}

function getTotalPages() {
    return Math.ceil(totalRecords / pageSize);
}

function updatePaginationControls() {
    const totalPages = getTotalPages();
    const paginationContainer = document.getElementById('pagination-controls');

    if (!paginationContainer) return;

    if (totalRecords === 0) {
        paginationContainer.innerHTML = '';
        return;
    }

    const startRecord = totalRecords === 0 ? 0 : (currentPage - 1) * pageSize + 1;
    const endRecord = Math.min(currentPage * pageSize, totalRecords);

    let paginationHTML = `
        <div class="pagination-wrapper">
            <div class="pagination-buttons">
                <button onclick="goToPage(${currentPage - 1})" ${currentPage === 1 ? 'disabled' : ''}>← Prev</button>
    `;

    const maxPagesToShow = 5;
    let startPage = Math.max(1, currentPage - Math.floor(maxPagesToShow / 2));
    let endPage = Math.min(totalPages, startPage + maxPagesToShow - 1);

    if (endPage - startPage < maxPagesToShow - 1) {
        startPage = Math.max(1, endPage - maxPagesToShow + 1);
    }

    if (startPage > 1) {
        paginationHTML += `<button onclick="goToPage(1)">1</button>`;
        if (startPage > 2) {
            paginationHTML += `<span style="padding: 0 8px; color: #64748b;">...</span>`;
        }
    }

    for (let i = startPage; i <= endPage; i++) {
        paginationHTML += `
            <button onclick="goToPage(${i})" class="${i === currentPage ? 'active' : ''}">${i}</button>
        `;
    }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) {
            paginationHTML += `<span style="padding: 0 8px; color: #64748b;">...</span>`;
        }
        paginationHTML += `<button onclick="goToPage(${totalPages})">${totalPages}</button>`;
    }

    paginationHTML += `
                <button onclick="goToPage(${currentPage + 1})" ${currentPage === totalPages ? 'disabled' : ''}>Next →</button>
            </div>
            <div style="color: #64748b; font-size: 14px; margin-left: 20px;">
                Showing ${startRecord}-${endRecord} of ${totalRecords} records
            </div>
        </div>
    `;

    paginationContainer.innerHTML = paginationHTML;
}

function goToPage(page) {
    const totalPages = getTotalPages();
    if (page < 1 || page > totalPages) return;

    currentPage = page;
    const type = getType();

    if (type === 'total') {
        showFullTable(getPaginatedData(currentDisplayData));
    } else {
        showTable(getPaginatedData(currentDisplayData));
    }

    updatePaginationControls();

    // Smooth scroll to top of table
    document.getElementById('budget-data-table').scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function changePageSize(newSize) {
    pageSize = parseInt(newSize);
    currentPage = 1;
    goToPage(1);
}

async function getExcelBudgetData() {
    let data = []
    showLoader();
    try {
        const url = `/api/auth/GetAllBudgetSummaryAmount?company=${companyId}&month=${getDate()}`
        const response = await fetch(url);
        if (!response.ok) return;
        const result = await response.json()
        data = result.data
        return data;
    } catch (err) {
        console.log("Error in Execution : ", err)
    } finally {
        hideLoader();
    }
}

async function convertToExcel() {
    const apiData = await getExcelBudgetData();

    if (!apiData || apiData.length === 0) {
        alert(`Summary not yet generated for: ${getDate()}`);
        return;
    }
    showLoader();
    try {

        const wb = XLSX.utils.book_new()
        const userData = apiData
        const uData = userData.map(user => ({
            'userId': user.userId,
            'userName': user.userName,
            'num_of_budget': user.budget.length
        }))



        const budgetData = [];
        userData.forEach(user => {
            if (user.budget && Array.isArray(user.budget)) {
                user.budget.forEach(budget => {
                    budgetData.push({
                        'userId': user.userId,
                        'userName': user.userName,
                        'Budget ID': budget.budgetId,
                        'Company': budget.company,
                        'Card Code': budget.cardCode,
                        'Card Name': budget.cardName,
                        'Doc Date': budget.docDate,
                        'Doc Total': parseFloat(budget.totalAmount),
                        'Status': budget.status,
                        'tenplateId': budget.header.templateId,
                        'totalStage': budget.header.totalStage,
                        'currentStageId': budget.header.currentStageId,
                        'currentStatus': budget.header.currentStatus
                    });
                });
            }
        });

        const lineData = [];
        userData.forEach(user => {
            if (user.budget && Array.isArray(user.budget)) {
                user.budget.forEach(budget => {
                    if (budget.lines && Array.isArray(budget.lines)) {
                        budget.lines.forEach(line => {
                            lineData.push({
                                'budgetId': budget.budgetId,
                                'Card Code': budget.cardCode,
                                'Card Name': budget.cardName,
                                'budget': line.budget,
                                'subBudget': line.subBudget,
                                'variety': line.variety,
                                'acctCode': line.acctCode,
                                'acctName': line.acctName,
                                'lineNum': line.lineNum,
                                'amount': line.amount,
                                'state': line.state,
                                'effectMonth': line.effectMonth,
                                'lineRemarks': line.lineRemarks,
                                'comments': line.comments

                            })
                        })
                    }
                });
            }
        });





        const userSheet = XLSX.utils.json_to_sheet(uData);
        const budgetSheet = XLSX.utils.json_to_sheet(budgetData)
        const lineSheet = XLSX.utils.json_to_sheet(lineData)

        XLSX.utils.book_append_sheet(wb, userSheet, 'User Summary');
        XLSX.utils.book_append_sheet(wb, budgetSheet, 'Budget Summary');
        XLSX.utils.book_append_sheet(wb, lineSheet, 'Budget Lines Summary')

        const fileName = `Budget_Export_${getDate()}.xlsx`;


        XLSX.writeFile(wb, fileName);

    } catch (err) {
        console.log('Error occurred:', err);
        alert('Failed to export. Check console for details.');
    } finally {
        hideLoader();
    }
}

function switchTab(tabName, clickedTab) {
    const tabButtons = document.querySelectorAll('.tab-button');

    tabButtons.forEach(button => {
        button.classList.remove('tab-active')
    })

    clickedTab.classList.add('tab-active');

    document.querySelectorAll('.form-tab').forEach(content => {
        content.style.display = 'none';
        content.classList.remove('active');
    });

    const selectedContent = document.getElementById(`${tabName}`);
    if (selectedContent) {
        selectedContent.style.display = 'block';
        selectedContent.classList.add('active');
    } else {
        console.error('Tab not found:', `${tabName}`);
    }
}

function getSearchType(inputValue) {
    const value = inputValue.trim();

    if (!value) return null;

    const firstChar = value.charAt(0);

    if (/\d/.test(firstChar)) {
        return 'number';
    } else if (/[a-zA-Z]/.test(firstChar)) {
        return 'string';
    } else {
        return 'special';
    }
}

async function getBudgetbyCompany() {
    const searchTerm = document.getElementById("search-form").value.toUpperCase();
    const monthTab = document.getElementById('month-tab2').value
    const yearTab = document.getElementById('year-tab2').value
    const month = `${monthTab}-${yearTab}`
    const type = getSearchType(searchTerm);

    if (!monthTab || !yearTab) {
        alert("Please select month and year");
        return;
    }

    showLoader();
    try {
        if (type === 'number') {
            console.log('Executing via docentry')
            const url = `/api/Reports/GetBudgetByCompany?company=${companyId}&month=${month}&docEntry=${searchTerm}`
            const result = await fetch(url);
            if (!result.ok) {
                console.log("No Result Found");
                return null;
            }
            const data = await result.json()
            return data;
        }
        else if (type === 'string') {
            const url = `/api/Reports/GetBudgetByCompany?company=${companyId}&month=${month}&cardName=${searchTerm}`
            const result = await fetch(url);
            if (!result.ok) {
                console.log("No Result Found");
                return null;
            }
            const data = await result.json()
            return data;
        }
    }
    catch (err) {
        console.error("Error Occurred:", err);
        alert("An error occurred while fetching data");
    } finally {
        hideLoader();
    }
}

async function displayBudget() {
    const table = document.getElementById('display-table')
    const searchTerm = document.getElementById("search-form").value.toUpperCase();
    const monthTab = document.getElementById('month-tab2').value
    const yearTab = document.getElementById('year-tab2').value
    const month = `${monthTab}-${yearTab}`
    const downloadButton = document.getElementById("get-excel-button");

    if (!searchTerm) {
        alert("Please enter a DocEntry or CardName");
        return;
    }

    tab2Data = await getBudgetbyCompany()

    table.innerHTML = '';

    if (!tab2Data || !tab2Data.data || !tab2Data.data.budgets || tab2Data.data.budgets.length === 0) {
        if (downloadButton.style.display === 'block') {
            downloadButton.style.display = 'none';
        }

        console.log('No data was found')
        alert(`No data found for ${searchTerm} in the month ${month}`);
        return;
    }

    downloadButton.style.display = 'block';

    tab2Data.data.budgets.forEach(budget => {
        const budgetCard = document.createElement('div')
        budgetCard.className = 'budget-card'

        budgetCard.innerHTML = `
        <div class="budget-card-header">
            <div class="card-expense">
                <h3>🆔 Budget ID: ${budget.budgetId || 'N/A'}</h3>
                <h3>💵 Total Amount</h3>
                <h1>₹${budget.totalAmount ? parseFloat(budget.totalAmount).toFixed(2) : '0.00'}</h1>
            </div>
            <div class="card-header-detail">
                <h4>📍 Company: ${budget.company}</h4>
                <h4>💻 CardCode: ${budget.cardCode}</h4>
                <h4>📆 DocDate: ${budget.docDate.slice(0, 10)}</h4>
            </div>
        </div>

        <div class="budget-card-name">
            <h4>👤 Owner: ${budget.budgetOwner || budget.ownerCode || 'N/A'}</h4>
            <h4>✅ Approver: ${budget.approverName || budget.approvalCode || 'N/A'}</h4>
            <h4>🏪 Vendor: ${budget.cardName}</h4>
        </div>
        
        <div class="budget-card-footer">
            <h4>💽 DocEntry: ${budget.docEntry || 'N/A'}</h4>
            <h4>📁 ${budget.objectName || 'N/A'}</h4>
        </div>
    `;
        table.appendChild(budgetCard);
    })
}

function getExceldoc() {
    if (!tab2Data || tab2Data.length === 0) {
        alert("No data available")
        return;
    }
    showLoader();
    try {
        const wb = XLSX.utils.book_new();
        const data = tab2Data.data.budgets;

        const workbookData = data.map(item => ({
            'Budget ID': item.budgetId,
            'Object Type': item.objType,
            'Company': item.company,
            'Company ID': item.companyId,
            'Doc Entry': item.docEntry,
            'Object Name': item.objectName,
            'Card Code': item.cardCode,
            'Card Name': item.cardName,
            'Doc Date': item.docDate,
            'Total Amount': item.totalAmount,
            'Current Month': item.currentMonth,
            'Budget Owner': item.budgetOwner || 'N/A',
            'Owner Code': item.ownerCode,
            'Approver Name': item.approverName || 'N/A',
            'Approval Code': item.approvalCode
        }));

        const workSheet = XLSX.utils.json_to_sheet(workbookData);
        XLSX.utils.book_append_sheet(wb, workSheet, 'Budget Summary');

        const timestamp = new Date().toISOString().slice(0, 10);
        const filename = `Budget_Export_${timestamp}.xlsx`;

        XLSX.writeFile(wb, filename);
    } catch (err) {
        console.log('Error occurred:', err);
        alert('Failed to export. Check console for details.');
    } finally {
        hideLoader();
    }
}




//MODAL FUNCTIONALITY 
function getCurrentTab() {
    const buttons = document.querySelectorAll(".budget-modal-tab-button");

    for (let btn of buttons) {
        if (btn.classList.contains('active')) {
            return btn;
        }
    }

    return null;
}

async function switchModalTab(status) {
    const tabs = document.querySelectorAll(".budget-modal-tab-content");
    const buttons = document.querySelectorAll(".budget-modal-tab-button");
    const displayTab = document.getElementById(`${status}-tab`);
    const displayBtn = document.getElementById(`${status}-btn`)

    const modalId = document.getElementById("budget-id");

    tabs.forEach(tab => tab.classList.remove('active'));
    buttons.forEach(btn => btn.classList.remove('active'));

    displayBtn.classList.add('active');
    displayTab.classList.add('active');

    const id = modalId.textContent.substring(4)

    if (status === 'pending') {
        displayModalCards(status, id, pending)
    } else if (status == 'rejected') {
        displayModalCards(status, id, rejected);
    } else if (status == 'approved') {
        displayModalCards(status, id, approved);
    }

}

async function openModal(id, user) {
    const modalHeader = document.getElementById("budget-modal-header-detail");
    modalHeader.innerHTML = `
        <h4 class='section-title'><i class="fa-regular fa-user" style="color: #0b2c4d;"></i> ${user}</h4>
        <h4 id = 'budget-id'>ID: ${id}</h4>
     
    `

    switchModalTab('pending')
    const modal = document.getElementById('budget-modal-background');
    if (!modal) {
        console.error('Modal not found');
        return;
    }
    modal.style.display = 'flex';
    //resetting modal back to pending
    displayModalCards('pending', id, pending)

}


async function getSingleData(status, id, data) {
    const date = getDate();
    const url = `/api/auth/get${status}budgetwithdetails?userId=${id}&company=${companyId}&month=${date}`;
    showLoader();
    try {
        const response = await fetch(url);
        if (!response.ok) {
            console.error(`Failed to fetch ${status} data`);
            return null;
        }
        const result = await response.json();
        const statusData = result.data;
        return statusData;
    } catch (err) {
        console.error("Error Occurred: ", err);
        return null;
    } finally {
        hideLoader();
    }

}
function closeModal() {
    const modal = document.getElementById('budget-modal-background');
    pending = []
    appproved = []
    rejected = []

    console.log("clearing data")
    console.log(`Approved : ${approved.length}`)
    console.log(`Pending : ${pending.length}`)
    console.log(`Rejectedd : ${rejected.length}`)

    if (modal) {
        modal.style.display = 'none';
    }
}


function statusId(status) {
    let id;
    if (status == 'Pending') {
        id = 'pending-status'
    } else if (status == 'Rejected') {
        id = 'rejected-status'
    } else if (status == 'Approved') {
        id = 'approved-status'
    }

    return id;
}

async function displayModalCards(tab, id, data) {

    console.log(`Before calling: ${data.length}`);
    if (data.length > 0) {
        console.log('Data hai bhai abhi')
    }

    const cardData = await getSingleData(tab, id, data);
    const displayTab = document.getElementById(`${tab}-tab`)
    console.log(`After calling: ${data.length}`);

    if (!cardData) {
        displayTab.innerHTML = `<h1 class= 'no-data-error'>No data found</h1>`
        return;
    }
    displayTab.innerHTML = ''
    cardData.forEach(card => {
        const sId = statusId(card.status)

        const budgetCard = document.createElement('div')
        budgetCard.className = 'budget-modal-card'
        budgetCard.innerHTML = `
            <div class = 'modal-card-header'>
                <div class = 'modal-expense-header'>
                    <h4>Expense Amount</h4>
                    <h4> &#8377; ${card.totalAmount}</h4>
                </div>

                <div class = 'modal-card-header-footer'>
                    <h1>ID: ${card.budgetId}</h1>
                    <h1>Branch: ${card.company}</h1>    
                    <h1>Date: ${card.docDate.substring(0, 10)}</h1>
                </div>
            </div>

    <div class="card-modal-tab-grid">
        <div class="tab-item">
            <h2>Document Name</h2>
            <h1>${card.objectName}</h1>
        </div>
    
    
        <div class="tab-item">
            <h2>Vendor</h2>
            <h1>${card.cardName}</h1>
        </div>             
           
    </div>
    
    <div class='card-flow-button-container'>
        <button onclick = 'openFlowModal(${card.budgetId})'>View Progress</button>
    </div>

        `
        displayTab.appendChild(budgetCard)
    })
}


async function getBudgetFlow(budgetId) {
    const url = `/api/auth/GetBudgetApprovalFlow?budgetId=${budgetId}`
    showLoader();
    try {
        const response = await fetch(url)
        if (!response.ok) return;

        const result = await response.json();
        const data = result.data
        flowData = data;

    } catch (err) {
        console.err(err)
    } finally {
        hideLoader();
    }
}


function openFlowModal(id) {
    const modal = document.getElementById('flow-modal-background')
    const text = document.getElementById('budget-flow-id')
    const heading = document.querySelectorAll('.flow-modal-heading')[0]

    console.log(heading)

    heading.innerHTML = `<h1 class = 'section-title'>📈 Approval Progress</h1>`
    text.style.cursor = 'pointer'
    modal.classList.add('active');

    displayApprovalFlow(id);
}


function getStatusAttributes(status) {
    let attr;
    if (status == 'A') {
        attr = {
            status: 'Approved',
            icon: '<i class="fa-regular fa-circle-check" style="color: #00a63e;"></i>',
            id: 'flow-status-approved'

        }
    } else if (status == 'R') {
        attr = {
            status: 'Rejected',
            icon: '<i class="fa-regular fa-circle-xmark" style="color: #ea1923;"></i>',
            id: 'flow-status-rejected'

        }
    } else {
        attr = {
            status: 'Pending',
            icon: '<i class="fa-regular fa-clock" style="color: #e17100;"></i>',
            id: 'flow-status-pending'

        }
    }

    return attr;
}

async function displayApprovalFlow(id) {
    const displayCont = document.getElementById('flow-modal-display-container')
    //fetching data 
    await getBudgetFlow(id);
    console.log(flowData)


    displayCont.innerHTML = ''
    flowData.forEach(stage => {
        const card = document.createElement('div')
        card.className = 'flow-card';
        const statusAttr = getStatusAttributes(stage.actionStatus)
        const date = stage.actionDate || 'NA'


        card.innerHTML = `
            <div class = 'flow-card-header'>
                <button class = 'flow-status-button' id = '${statusAttr.id}'>${statusAttr.icon}</button>
            </div>

            <div class = 'flow-card-main'>
                <div class = 'flow-tag-cont'>
                    <h1 class = 'flow-tag' >Stage ${stage.priority}</h1>
                    <h1 class = 'flow-tag' id = '${statusAttr.id}'>${statusAttr.status}</h1>
                </div>
            
                <div class = 'flow-card-main-content'>
                    <h4 class='flow-card-item'><i class="fa-regular fa-user" style="color: #45556c;"></i> <span>Assigned To</span>:  ${stage.assignedTo}</h4>
                    <h4 class='flow-card-item'><i class="fa-regular fa-calendar-days" style="color: #58667b;"></i>  <span>Action Date</span>:  ${stage.actionDate ? stage.actionDate.substring(0, 10) : "Not Assigned"}</i></h4>
                    <h4 class='flow-card-item'><i class="fa-regular fa-file-lines" style="color: #45556c;"></i>  <span>Description</span>:  ${stage.description ? stage.description : 'NA'}</i></h4>
                </div>

                 <div class = 'flow-card-footer'>
                    <h4 class='flow-card-footer-item'>${stage.approvalRequired === 1 ? "Approval Required" : "Rejection Required"}</h4>
                </div>
            </div>
    
            </div>
        `
        displayCont.appendChild(card)
    })

}


/*async function displayApprovalFlow(id) {
    const displayCont = document.getElementById('flow-modal-display-container');

    // Fetch data
    await getBudgetFlow(id);
    console.log(flowData);

    // Clear old content
    displayCont.innerHTML = '';

    // Create table element
    const table = document.createElement('table');
    table.className = 'flow-table';

    // Table Header
    table.innerHTML = `
        <thead>
            <tr>
                <th>Stage</th>
                <th>Status</th>
                <th>Assigned To</th>
                <th>Action Date</th>
            </tr>
        </thead>
        <tbody id="flow-tbody"></tbody>
    `;

    displayCont.appendChild(table);

    const tbody = table.querySelector('#flow-tbody');

    // Loop through flow data
    flowData.forEach(stage => {
        const statusAttr = getStatusAttributes(stage.actionStatus);

        const row = document.createElement('tr');
        row.innerHTML = `
            <td>Stage ${stage.priority}</td>
            <td><span class="flow-status-badge" id="${statusAttr.id}">${statusAttr.status}</span></td>
            <td>${stage.assignedTo}</td>
            <td>${stage.actionDate ? stage.actionDate.substring(0, 10) : "Not Assigned"}</td>
        `;

        tbody.appendChild(row);
    });
}
*/
function closeFlowModal() {
    const modal = document.getElementById('flow-modal-background')
    modal.classList.remove('active');

    flowData = [];
}


function initializeForm() {
    setupMonths();
    setupYears();
    setupTypes();
    setupSearch();
    setDefaultValues();
    setupMonthsTab2();
    setupYearsTab2();
    setDefaultValuesTab2();
}

document.addEventListener('DOMContentLoaded', () => {
    console.log('Budget Approval file connected')
    initializeForm();
})