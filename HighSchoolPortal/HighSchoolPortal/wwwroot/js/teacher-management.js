// wwwroot/js/teacher-management.js
// Teacher Management specific functions
function confirmDelete(teacherName) {
    return confirm(`Are you sure you want to delete "${teacherName}"?\nThis action cannot be undone.`);
}

// Filter functionality for teacher management
function initializeTeacherFilters() {
    const searchInput = document.getElementById('searchInput');
    const deptFilter = document.getElementById('departmentFilter');
    const statusFilter = document.getElementById('statusFilter');

    if (!searchInput || !deptFilter || !statusFilter) return;

    function filterTeachers() {
        const searchTerm = searchInput.value.toLowerCase();
        const dVal = deptFilter.value;
        const sVal = statusFilter.value;
        const rows = document.querySelectorAll('#teachersTable tbody tr');

        rows.forEach(row => {
            const name = row.cells[0].textContent.toLowerCase();
            const idElements = row.querySelector('.text-muted');
            const id = idElements ? idElements.textContent.toLowerCase() : '';
            const dept = row.getAttribute('data-department');
            const status = row.getAttribute('data-status');

            let show = true;

            if (searchTerm && !name.includes(searchTerm) && !id.includes(searchTerm)) {
                show = false;
            }
            if (dVal && dept !== dVal) show = false;
            if (sVal && status !== sVal) show = false;

            row.style.display = show ? '' : 'none';
        });
    }

    if (searchInput) searchInput.addEventListener('keyup', filterTeachers);
    if (deptFilter) deptFilter.addEventListener('change', filterTeachers);
    if (statusFilter) statusFilter.addEventListener('change', filterTeachers);
}

function resetFilters() {
    const searchInput = document.getElementById('searchInput');
    const deptFilter = document.getElementById('departmentFilter');
    const statusFilter = document.getElementById('statusFilter');

    if (searchInput) searchInput.value = '';
    if (deptFilter) deptFilter.value = '';
    if (statusFilter) statusFilter.value = '';

    // Trigger filter if filterTeachers function exists in current page
    if (typeof filterTeachers === 'function') {
        filterTeachers();
    }
}

// Initialize teacher filters when document is ready
$(document).ready(function () {
    initializeTeacherFilters();
});