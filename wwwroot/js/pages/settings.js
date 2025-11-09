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

function navigateToUserManagement() {
    // Navigate to user management page
    window.location.href = '/Settings/UserManagement';
}

function navigateToChangeWorkflow() {
    // Navigate to change workflow page
    window.location.href = '/Settings/ChangeWorkflow';
}

function navigateToBypassStage() {
    // Navigate to bypass stage page
    window.location.href = '/BypassStage/Index';
}
function navigateToAccessControl() {
    // Navigate to access control page
    window.location.href = '/AccessControl/Index';
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