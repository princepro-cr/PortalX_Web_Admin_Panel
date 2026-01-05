// wwwroot/js/site.js
// Sidebar toggle functionality
document.addEventListener('DOMContentLoaded', function () {
    const sidebar = document.getElementById('sidebar');
    const mainContent = document.getElementById('mainContent');
    const sidebarToggle = document.getElementById('sidebarToggle');
    const mobileSidebarToggle = document.getElementById('mobileSidebarToggle');

    // Load sidebar state from localStorage
    const isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
    if (isCollapsed) {
        sidebar.classList.add('collapsed');
        mainContent.classList.add('expanded');
    }

    // Desktop sidebar toggle
    sidebarToggle.addEventListener('click', function () {
        sidebar.classList.toggle('collapsed');
        mainContent.classList.toggle('expanded');
        localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
    });

    // Mobile sidebar toggle
    mobileSidebarToggle.addEventListener('click', function () {
        sidebar.classList.toggle('collapsed');
        mainContent.classList.toggle('expanded');
        localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
    });

    // Auto-collapse on mobile
    function handleResize() {
        if (window.innerWidth <= 768) {
            sidebar.classList.add('collapsed');
            mainContent.classList.add('expanded');
        } else {
            // Restore saved state on desktop
            const isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';
            if (isCollapsed) {
                sidebar.classList.add('collapsed');
                mainContent.classList.add('expanded');
            } else {
                sidebar.classList.remove('collapsed');
                mainContent.classList.remove('expanded');
            }
        }
    }

    window.addEventListener('resize', handleResize);
    handleResize(); // Initial check

    // Load role-based counts
    loadRoleBasedCounts();
});

function loadRoleBasedCounts() {
    const userRole = document.body.dataset.userRole || '';

    if (userRole === 'teacher') {
        // Load teacher's student count
        $.ajax({
            url: '/Teacher/GetStudentCount',
            type: 'GET',
            success: function (data) {
                if (data && data.count > 0) {
                    $('#studentCountBadge').text(data.count).show();
                }
            }
        });
    } else if (userRole === 'hr') {
        // Load HR student count
        $.ajax({
            url: '/HR/GetStudentCount',
            type: 'GET',
            success: function (data) {
                if (data && data.count > 0) {
                    $('#hrStudentCountBadge').text(data.count).show();
                }
            }
        });

        // Load HR teacher count
        $.ajax({
            url: '/HR/GetTeacherCount',
            type: 'GET',
            success: function (data) {
                if (data && data.count > 0) {
                    $('#teacherCountBadge').text(data.count).show();
                }
            }
        });
    }
}

// Initialize tooltips
$(document).ready(function () {
    $('[data-bs-toggle="tooltip"]').tooltip();

    // Auto-dismiss alerts after 5 seconds
    setTimeout(function () {
        $('.alert').alert('close');
    }, 5000);

    // Add smooth scrolling
    $('a[href^="#"]').on('click', function (event) {
        if (this.hash !== "") {
            event.preventDefault();
            const hash = this.hash;
            $('html, body').animate({
                scrollTop: $(hash).offset().top - 80
            }, 800);
        }
    });
});

// Format numbers with commas
function formatNumber(num) {
    return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

// Calculate grade based on score
function calculateGrade(score) {
    if (score >= 90) return 'A';
    if (score >= 80) return 'B';
    if (score >= 70) return 'C';
    if (score >= 60) return 'D';
    return 'F';
}

// Get grade color class
function getGradeClass(score) {
    if (score >= 90) return 'grade-a';
    if (score >= 80) return 'grade-b';
    if (score >= 70) return 'grade-c';
    if (score >= 60) return 'grade-d';
    return 'grade-f';
}