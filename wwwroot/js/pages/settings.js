// Settings Page JavaScript Functions

function navigateToWorkflow() {
    // Navigate to workflow management page
    window.location.href = '/Settings/WorkflowManagement';
}

function navigateToApprover() {
    // Navigate to approver management page
    window.location.href = '/Settings/ApproverManagement';
}

function navigateToRoleManagement() {
    // Navigate to role management page
    window.location.href = '/Settings/RoleManagement';
}

function showComingSoon(featureName) {
    Swal.fire({
        icon: 'info',
        title: 'Coming Soon!',
        text: `${featureName} feature is under development and will be available in future updates.`,
        confirmButtonText: 'OK',
        confirmButtonColor: '#3b82f6'
    });
}