// Settings Page JavaScript Functions

function navigateToWorkflow() {
    // Navigate to workflow management page
    window.location.href = '/Settings/WorkflowManagement';
}

function navigateToLevel() {
    // Navigate to level management page
    window.location.href = '/Settings/LevelManagement';
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