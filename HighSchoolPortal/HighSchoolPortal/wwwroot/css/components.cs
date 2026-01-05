/* wwwroot/css/components.css */
:root {
    --primary-blue: #4361ee;
    --secondary - purple: #7209b7;
    --bg - light: #f8f9fd;
    --glass: rgba(255, 255, 255, 0.9);

/* Teacher-specific colors */
--teacher - primary: #06d6a0;
    --teacher - secondary: #118ab2;
    --teacher - light: #caf0f8;
    --teacher - dark: #073b4c;
    
    /* HR-specific colors */
    --hr - primary: #ef476f;
    --hr - secondary: #ffd166;
    --hr - light: #ffefd6;
    --hr - dark: #9d174d;
    
    /* Status colors */
    --success - color: #06d6a0;
    --warning - color: #ffd166;
    --danger - color: #ef476f;
    --info - color: #118ab2;
    --primary - color: var(--primary - blue);
--secondary - color: var(--secondary - purple);
}

/* Modern Header */
.page - header - modern {
background: white;
padding: 1.5rem 2rem;
    border - radius: 16px;
    box - shadow: 0 4px 20px rgba(0,0,0,0.03);
    margin - bottom: 2rem;
}

.text - gradient {
background: linear - gradient(45deg, var(--primary - blue), var(--secondary - purple));
    -webkit - background - clip: text;
    -webkit - text - fill - color: transparent;
    background - clip: text;
    font - weight: 800;
}

/* Card Styles */
.card {
    border: none;
border - radius: 12px;
box - shadow: 0 4px 12px rgba(0,0,0,0.08);
transition: transform 0.3s, box-shadow 0.3s;
margin - bottom: 1.5rem;
}

.card: hover {
transform: translateY(-2px);
    box - shadow: 0 8px 24px rgba(0,0,0,0.12);
}

.card - header {
background: linear - gradient(135deg, var(--primary - color) 0 %, var(--secondary - color) 100 %);
color: white;
    border - radius: 12px 12px 0 0!important;
border: none;
padding: 1rem 1.5rem;
}

/* Stats Grid */
.mini - stat - card {
border: none;
    border - radius: 12px;
padding: 1rem;
transition: all 0.3s ease;
background: white;
    border - left: 4px solid var(--primary - blue);
height: 100 %;
    box - shadow: 0 4px 12px rgba(0,0,0,0.08);
}

.mini - stat - card:hover {
    transform: translateY(-3px);
box - shadow: 0 8px 15px rgba(0,0,0,0.05);
}

/* Filter Bar */
.filter - section {
background: var(--glass);
    backdrop - filter: blur(10px);
border: 1px solid rgba(255, 255, 255, 0.2);
    border - radius: 12px;
padding: 1.25rem;
    box - shadow: 0 4px 20px rgba(0,0,0,0.03);
}

/* Main Card */
.main - card {
border: none;
    border - radius: 16px;
    box - shadow: 0 10px 30px rgba(0,0,0,0.04);
overflow: hidden;
}

/* Table Styling */
.table thead
{
    background: #f8f9ff;
}

.table thead th {
    font-weight: 600;
text - transform: uppercase;
font - size: 0.75rem;
letter - spacing: 0.5px;
color: #5a6a85;
    border: none;
padding: 1rem;
}

.teacher - row,
.student - row {
transition: background 0.2s;
}

.teacher - row:hover,
.student-row:hover {
    background-color: #fcfdff !important;
}

/* Action Buttons */
.btn - circle {
width: 35px;
height: 35px;
    border - radius: 50 %;
display: flex;
    align - items: center;
    justify - content: center;
transition: 0.2s;
border: none;
}

.badge - soft {
padding: 0.5em 0.8em;
    font - weight: 600;
    border - radius: 6px;
}

/* GPA Indicator */
.gpa - indicator {
width: 8px;
height: 8px;
    border - radius: 50 %;
display: inline - block;
    margin - right: 5px;
}

/* Student Card Styles */
.student - card {
border: 1px solid #e9ecef;
    border - radius: 10px;
transition: all 0.3s ease;
}

.student - card:hover {
    border-color: var(--teacher - primary);
box - shadow: 0 5px 15px rgba(6, 214, 160, 0.1);
}

.student - avatar {
width: 80px;
height: 80px;
    object-fit: cover;
border: 3px solid var(--teacher - light);
}

/* Teacher-specific styles */
.teacher - gradient {
background: linear - gradient(135deg, var(--teacher - primary) 0 %, var(--teacher - secondary) 100 %)!important;
}

.teacher - gradient - bg {
background: linear - gradient(135deg, var(--teacher - primary) 0 %, var(--teacher - secondary) 100 %)!important;
}

.teacher - gradient - text {
background: linear - gradient(135deg, var(--teacher - primary) 0 %, var(--teacher - secondary) 100 %);
    -webkit - background - clip: text;
    -webkit - text - fill - color: transparent;
    background - clip: text;
}

.btn - teacher - primary {
background: linear - gradient(135deg, var(--teacher - primary) 0 %, var(--teacher - secondary) 100 %);
border: none;
color: white;
transition: all 0.3s ease;
}

.btn - teacher - primary:hover {
    background: linear - gradient(135deg, var(--teacher - dark) 0 %, var(--teacher - secondary) 100 %);
color: white;
transform: translateY(-1px);
box - shadow: 0 4px 12px rgba(6, 214, 160, 0.2);
}

.btn - teacher - secondary {
background: white;
border: 2px solid var(--teacher - primary);
color: var(--teacher - primary);
transition: all 0.3s ease;
}

.btn - teacher - secondary:hover {
    background: var(--teacher - light);
border - color: var(--teacher - dark);
color: var(--teacher - dark);
}

/* HR-specific styles */
.hr - gradient {
background: linear - gradient(135deg, var(--hr - primary) 0 %, var(--hr - secondary) 100 %)!important;
}

.hr - gradient - bg {
background: linear - gradient(135deg, var(--hr - primary) 0 %, var(--hr - secondary) 100 %)!important;
}

.hr - gradient - text {
background: linear - gradient(135deg, var(--hr - primary) 0 %, var(--hr - secondary) 100 %);
    -webkit - background - clip: text;
    -webkit - text - fill - color: transparent;
    background - clip: text;
}

.btn - hr - primary {
background: linear - gradient(135deg, var(--hr - primary) 0 %, var(--hr - secondary) 100 %);
border: none;
color: white;
}

.btn - hr - primary:hover {
    background: linear - gradient(135deg, var(--hr - dark) 0 %, var(--hr - secondary) 100 %);
color: white;
}

/* Grade Badges */
.badge - present {
    background - color: var(--success - color);
color: white;
}

.badge - absent {
    background - color: var(--danger - color);
color: white;
}

.badge - late {
    background - color: var(--warning - color);
color: #333;
}

.badge - excused {
    background - color: var(--info - color);
color: white;
}

/* Attendance Status */
.status - present {
color: var(--success - color);
}

.status - absent {
color: var(--danger - color);
}

.status - late {
color: var(--warning - color);
}

.status - excused {
color: var(--info - color);
}

/* Performance Indicators */
.grade - a {
color: var(--success - color);
}

.grade - b {
color: #28a745;
}

.grade - c {
color: var(--warning - color);
}

.grade - d {
color: #fd7e14;
}

.grade - f {
color: var(--danger - color);
}

/* Statistics Cards */
.stat - card {
    border - radius: 12px;
border: none;
    box - shadow: 0 4px 12px rgba(0,0,0,0.08);
transition: all 0.3s ease;
}

.stat - card:hover {
    transform: translateY(-5px);
box - shadow: 0 8px 24px rgba(0,0,0,0.12);
}

.stat - number {
    font - weight: 700;
    font - size: 2rem;
}

.stat - label {
color: #6c757d;
    font - size: 0.875rem;
}

.feature - icon {
width: 60px;
height: 60px;
    border - radius: 12px;
display: flex;
    align - items: center;
    justify - content: center;
color: white;
    font - size: 1.5rem;
}

.feature - icon - sm {
width: 40px;
height: 40px;
    border - radius: 8px;
display: flex;
    align - items: center;
    justify - content: center;
color: white;
    font - size: 1rem;
}

/* Utility Classes */
.bg - teacher - light {
    background - color: var(--teacher - light)!important;
}

.text - teacher - primary {
color: var(--teacher - primary)!important;
}

.border - teacher {
    border - color: var(--teacher - primary)!important;
}

.bg - success - subtle {
    background - color: rgba(6, 214, 160, 0.1)!important;
}

.bg - warning - subtle {
    background - color: rgba(255, 209, 102, 0.1)!important;
}

.bg - danger - subtle {
    background - color: rgba(239, 71, 111, 0.1)!important;
}

.bg - info - subtle {
    background - color: rgba(17, 138, 178, 0.1)!important;
}

.bg - primary - subtle {
    background - color: rgba(67, 97, 238, 0.1)!important;
}

/* Animations */
.animate - fade -in {
animation: fadeIn 0.5s ease-in;
}

.animate - slide - up {
animation: slideUp 0.5s ease-out;
}

@keyframes fadeIn
{
    from
    {
    opacity: 0;
    }
    to
    {
    opacity: 1;
    }
}

@keyframes slideUp
{
    from
    {
    opacity: 0;
    transform: translateY(20px);
    }
    to
    {
    opacity: 1;
    transform: translateY(0);
    }
}

/* Responsive adjustments */
@media(max - width: 768px) {
    .page - header - modern {
    padding: 1rem;
    }

    .filter - section {
    padding: 1rem;
    }

    .stat - number {
        font - size: 1.5rem;
    }
    
    .btn - circle {
    width: 30px;
    height: 30px;
    }
    
    .table - responsive {
        font - size: 0.85rem;
    }
}

/* Additional improvements for the student details page */
.bg - orange - subtle {
    background - color: rgba(253, 126, 20, 0.1)!important;
}

.text - orange {
color: #fd7e14 !important;
}

.rounded - 3 {
    border - radius: 0.75rem!important;
}

.vr {
    width: 1px;
opacity: 0.3;
}

/* Pagination improvements */
.pagination.page - link {
border: none;
color: var(--primary - blue);
margin: 0 2px;
    border - radius: 8px;
}

.pagination.page - item.active.page - link {
background: linear - gradient(135deg, var(--primary - color) 0 %, var(--secondary - color) 100 %);
color: white;
}

.pagination.page - link:hover {
    background-color: rgba(67, 97, 238, 0.1);
}