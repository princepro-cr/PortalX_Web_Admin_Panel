// wwwroot/js/student-management.js
// Student Management specific functions
function confirmDelete(studentName) {
    return confirm(`Are you sure you want to delete "${studentName}"?\nThis action cannot be undone.`);
}

// Filter functionality for student management
function initializeStudentFilters() {
    const searchInput = document.getElementById('searchInput');
    const gradeFilter = document.getElementById('gradeFilter');
    const statusFilter = document.getElementById('statusFilter');

    if (!searchInput || !gradeFilter || !statusFilter) return;

    function filterStudents() {
        const searchTerm = searchInput.value.toLowerCase();
        const gFilter = gradeFilter.value;
        const sFilter = statusFilter.value;
        const rows = document.querySelectorAll('#studentsTable tbody tr');

        rows.forEach(row => {
            const name = row.cells[0].textContent.toLowerCase();
            const studentId = row.querySelector('.badge').textContent.toLowerCase();
            const email = row.cells[1].textContent.toLowerCase();
            const grade = row.getAttribute('data-grade');
            const status = row.getAttribute('data-status');

            let show = true;

            if (searchTerm && !name.includes(searchTerm) && !studentId.includes(searchTerm) && !email.includes(searchTerm)) {
                show = false;
            }
            if (gFilter && grade !== gFilter) show = false;
            if (sFilter && status !== sFilter) show = false;

            row.style.display = show ? '' : 'none';
        });
    }

    if (searchInput) searchInput.addEventListener('keyup', filterStudents);
    if (gradeFilter) gradeFilter.addEventListener('change', filterStudents);
    if (statusFilter) statusFilter.addEventListener('change', filterStudents);

    return filterStudents;
}

function resetFilters() {
    const searchInput = document.getElementById('searchInput');
    const gradeFilter = document.getElementById('gradeFilter');
    const statusFilter = document.getElementById('statusFilter');

    if (searchInput) searchInput.value = '';
    if (gradeFilter) gradeFilter.value = '';
    if (statusFilter) statusFilter.value = '';

    // Trigger filter if filterStudents function exists
    if (typeof window.filterStudents === 'function') {
        window.filterStudents();
    }
}

// Initialize student filters when document is ready
$(document).ready(function () {
    const filterFunc = initializeStudentFilters();
    if (filterFunc) {
        window.filterStudents = filterFunc;
    }
});