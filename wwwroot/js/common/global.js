// Global JavaScript Functions

function showComingSoon(featureName) {
    Swal.fire({
        icon: 'info',
        title: 'Coming Soon!',
        text: `${featureName} feature is under development and will be available in future updates.`,
        confirmButtonText: 'OK',
        confirmButtonColor: '#3b82f6'
    });
}