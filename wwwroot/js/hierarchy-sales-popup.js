/* Sales hierarchy stat card popup logic — served as static file for instant updates */
(function () {
    function getSalesPopupConfig(level) {
        var SALES_RAW_DATA = window.SALES_RAW_DATA || [];
        if (!SALES_RAW_DATA.length) return null;
        var cfgs = {
            h1: {
                title: 'Sales H1 Leaders', icon: 'bi-person-badge-fill', gradient: '#2563eb,#1d4ed8',
                heads: ['#', 'Name', 'Code', 'H2 Count'],
                rows: (function () {
                    var r = [];
                    SALES_RAW_DATA.forEach(function (h1) { r.push([h1.h1Name || '\u2014', h1.h1Code || '\u2014', (h1.h2List || []).length]); });
                    return r;
                })()
            },
            h2: {
                title: 'Sales H2 Leaders', icon: 'bi-diagram-2-fill', gradient: '#059669,#047857',
                heads: ['#', 'Name', 'Code', 'Under H1', 'H3 Count'],
                rows: (function () {
                    var r = [], seen = {};
                    SALES_RAW_DATA.forEach(function (h1) {
                        (h1.h2List || []).forEach(function (h2) {
                            var k = h2.h2Code || h2.h2Name;
                            if (!seen[k]) { seen[k] = 1; r.push([h2.h2Name || '\u2014', h2.h2Code || '\u2014', h1.h1Name || '\u2014', (h2.h3List || []).length]); }
                        });
                    });
                    return r;
                })()
            },
            h3: {
                title: 'Sales H3 (Regions/States)', icon: 'bi-diagram-3-fill', gradient: '#d97706,#b45309',
                heads: ['#', 'Name', 'Code', 'Under H2', 'H4 Count'],
                rows: (function () {
                    var r = [], seen = {};
                    SALES_RAW_DATA.forEach(function (h1) {
                        (h1.h2List || []).forEach(function (h2) {
                            (h2.h3List || []).forEach(function (h3) {
                                var k = h3.h3Code || h3.h3Name;
                                if (!seen[k]) { seen[k] = 1; r.push([h3.h3Name || '\u2014', h3.h3Code || '\u2014', h2.h2Name || '\u2014', (h3.h4List || []).length]); }
                            });
                        });
                    });
                    return r;
                })()
            },
            h4: {
                title: 'Sales H4 (Designations)', icon: 'bi-people-fill', gradient: '#9333ea,#7c3aed',
                heads: ['#', 'Name', 'Code', 'Under H3', 'Employees'],
                rows: (function () {
                    var r = [], seen = {};
                    SALES_RAW_DATA.forEach(function (h1) {
                        (h1.h2List || []).forEach(function (h2) {
                            (h2.h3List || []).forEach(function (h3) {
                                (h3.h4List || []).forEach(function (h4) {
                                    var k = h4.h4Code || h4.h4Name;
                                    if (!seen[k]) {
                                        seen[k] = 1;
                                        var cnt = (h4.groups || []).reduce(function (s, g) { return s + (g.employees || []).length; }, 0);
                                        r.push([h4.h4Name || '\u2014', h4.h4Code || '\u2014', h3.h3Name || '\u2014', cnt]);
                                    }
                                });
                            });
                        });
                    });
                    return r;
                })()
            },
            emp: {
                title: 'All Sales Employees', icon: 'bi-person-fill', gradient: '#15803d,#16a34a',
                heads: ['#', 'Code', 'Name', 'Group', 'State', 'Designation'],
                rows: (function () {
                    var r = [];
                    SALES_RAW_DATA.forEach(function (h1) {
                        (h1.h2List || []).forEach(function (h2) {
                            (h2.h3List || []).forEach(function (h3) {
                                (h3.h4List || []).forEach(function (h4) {
                                    (h4.groups || []).forEach(function (g) {
                                        (g.employees || []).forEach(function (emp) {
                                            r.push([emp.empCode || '\u2014', emp.empName || '\u2014', g.groupName || '\u2014', emp.state || '\u2014', emp.designation || '\u2014']);
                                        });
                                    });
                                });
                            });
                        });
                    });
                    return r;
                })()
            }
        };
        return cfgs[level] || null;
    }

    window.showSalesStatPopup = function (level) {
        var SALES_RAW_DATA = window.SALES_RAW_DATA || [];
        if (!SALES_RAW_DATA.length) {
            if (typeof window.showAlert === 'function') window.showAlert('warning', 'Sales data not loaded yet.');
            return;
        }
        var cfg = getSalesPopupConfig(level);
        if (!cfg) return;
        if (typeof window.showCustomStatTable === 'function') {
            window.showCustomStatTable(cfg);
            var titleEl = document.getElementById('statModalTitle');
            if (titleEl) titleEl.style.color = '#fff';
        }
    };

    /* Event delegation — works regardless of how onclick is wired */
    document.addEventListener('click', function (e) {
        var target = e.target;
        while (target && target !== document) {
            if (target.classList && target.classList.contains('esc')) {
                var grid = document.getElementById('salesTeamGrid');
                if (grid && grid.contains(target)) {
                    var level = target.getAttribute('data-level');
                    if (level) { window.showSalesStatPopup(level); return; }
                }
            }
            target = target.parentElement;
        }
    });
}());
