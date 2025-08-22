// Workflow Management Page JavaScript

$(document).ready(function() {
    // Handle delete workflow button clicks first (before row clicks)
    $(document).on('click', '.delete-workflow-btn', function(e) {
        e.preventDefault();
        e.stopPropagation();
        
        var workflowId = $(this).data('workflow-id');
        var workflowName = $(this).data('workflow-name');
        
        confirmDeleteWorkflow(workflowId, workflowName);
    });
    
    // Handle clickable rows (exclude clicks on action buttons)
    $(document).on('click', '.clickable-row', function(e) {
        // Don't navigate if clicking on a button or its children
        if ($(e.target).closest('button').length > 0) {
            return;
        }
        
        var href = $(this).data('href');
        if (href) {
            window.location.href = href;
        }
    });
    
    // Auto-format workflow name input for add form
    $('#workflowName').on('input', function() {
        var value = $(this).val().toUpperCase();
        $(this).val(value);
    });
    
    // Auto-format workflow name input for edit form
    $('#editWorkflowName').on('input', function() {
        var value = $(this).val().toUpperCase();
        $(this).val(value);
    });
});

// Add Workflow Form Submit
document.getElementById('addWorkflowForm').addEventListener('submit', function(e) {
    e.preventDefault();
    
    // Format workflow name with WF_ prefix
    var workflowNameInput = $('#workflowName').val().trim().toUpperCase();
    
    // Validate workflow name input
    if (!workflowNameInput) {
        Swal.fire({
            icon: 'warning',
            title: 'Validation Error!',
            text: 'Please enter a workflow identifier.',
            confirmButtonColor: '#dc2626'
        });
        return;
    }
    
    // Remove WF_ prefix if user accidentally typed it
    if (workflowNameInput.startsWith('WF_')) {
        workflowNameInput = workflowNameInput.substring(3);
    }
    
    var fullWorkflowName = 'WF_' + workflowNameInput;
    
    const formData = new FormData(this);
    
    // Update the workflow name with prefix
    formData.set('workflowName', fullWorkflowName);
    
    // Convert checkbox value
    formData.set('isActive', document.getElementById('isActive').checked);
    
    fetch(window.workflowManagementUrls?.create || '/WorkflowManagement/Create', {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            Swal.fire({
                icon: 'success',
                title: 'Success!',
                text: data.message,
                confirmButtonColor: '#3b82f6'
            }).then(() => {
                location.reload();
            });
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Error!',
                text: data.message,
                confirmButtonColor: '#3b82f6'
            });
        }
    })
    .catch(error => {
        console.error('Error:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error!',
            text: 'An error occurred while adding the workflow.',
            confirmButtonColor: '#3b82f6'
        });
    });
});

// Edit Workflow Form Submit
document.getElementById('editWorkflowForm').addEventListener('submit', function(e) {
    e.preventDefault();
    
    // Format workflow name with WF_ prefix
    var workflowNameInput = $('#editWorkflowName').val().trim().toUpperCase();
    
    // Validate workflow name input
    if (!workflowNameInput) {
        Swal.fire({
            icon: 'warning',
            title: 'Validation Error!',
            text: 'Please enter a workflow identifier.',
            confirmButtonColor: '#dc2626'
        });
        return;
    }
    
    // Remove WF_ prefix if user accidentally typed it
    if (workflowNameInput.startsWith('WF_')) {
        workflowNameInput = workflowNameInput.substring(3);
    }
    
    var fullWorkflowName = 'WF_' + workflowNameInput;
    var workflowId = document.getElementById('editWorkflowId').value;
    
    const formData = new FormData();
    formData.append('id', workflowId);
    formData.append('workflowName', fullWorkflowName);
    formData.append('desc', document.getElementById('editDesc').value.trim());
    formData.append('isActive', document.getElementById('editIsActive').checked);
    
    fetch((window.workflowManagementUrls?.edit || '/WorkflowManagement/Edit') + '/' + workflowId, {
        method: 'POST',
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            // Close modal
            var editModal = bootstrap.Modal.getInstance(document.getElementById('editWorkflowModal'));
            editModal.hide();
            
            Swal.fire({
                icon: 'success',
                title: 'Success!',
                text: data.message,
                confirmButtonColor: '#3b82f6'
            }).then(() => {
                location.reload();
            });
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Error!',
                text: data.message,
                confirmButtonColor: '#3b82f6'
            });
        }
    })
    .catch(error => {
        console.error('Error:', error);
        Swal.fire({
            icon: 'error',
            title: 'Error!',
            text: 'An error occurred while updating the workflow.',
            confirmButtonColor: '#3b82f6'
        });
    });
});

// Confirm Delete Function
function confirmDeleteWorkflow(workflowId, workflowName) {
    Swal.fire({
        title: 'Are you sure?',
        text: `You are about to delete workflow "${workflowName}". This action cannot be undone!`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            // Show loading state
            Swal.fire({
                title: 'Deleting...',
                text: 'Please wait while we delete the workflow.',
                allowOutsideClick: false,
                allowEscapeKey: false,
                showConfirmButton: false,
                willOpen: () => {
                    Swal.showLoading();
                }
            });
            
            // Perform AJAX delete action
            $.ajax({
                url: (window.workflowManagementUrls?.delete || '/WorkflowManagement/Delete') + '/' + workflowId,
                type: 'GET', // Controller expects GET request
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                success: function(response) {
                    console.log('Delete response:', response);
                    
                    // Handle different response types
                    if (typeof response === 'string') {
                        // If response is HTML (redirect), assume success
                        Swal.fire({
                            icon: 'success',
                            title: 'Deleted!',
                            text: `Workflow "${workflowName}" has been deleted successfully.`,
                            confirmButtonColor: '#3b82f6'
                        }).then(() => {
                            location.reload();
                        });
                    } else if (response && response.success === false) {
                        // Handle error response
                        Swal.fire({
                            icon: 'error',
                            title: 'Cannot Delete Workflow',
                            text: response.error || 'Cannot delete workflow.',
                            confirmButtonColor: '#dc2626'
                        });
                    } else if (response && response.success === true) {
                        // Success - workflow deleted
                        Swal.fire({
                            icon: 'success',
                            title: 'Deleted!',
                            text: `Workflow "${workflowName}" has been deleted successfully.`,
                            confirmButtonColor: '#3b82f6'
                        }).then(() => {
                            location.reload();
                        });
                    } else {
                        // Default to success if no clear error
                        Swal.fire({
                            icon: 'success',
                            title: 'Deleted!',
                            text: `Workflow "${workflowName}" has been deleted successfully.`,
                            confirmButtonColor: '#3b82f6'
                        }).then(() => {
                            location.reload();
                        });
                    }
                },
                error: function(xhr, status, error) {
                    console.log('Delete error:', xhr, status, error);
                    
                    // Handle error response
                    var errorMessage = 'An error occurred while deleting the workflow.';
                    if (xhr.responseJSON && xhr.responseJSON.error) {
                        errorMessage = xhr.responseJSON.error;
                    } else if (xhr.responseText) {
                        // Check if response contains error message
                        if (xhr.responseText.includes('Cannot delete workflow')) {
                            errorMessage = 'Cannot delete workflow. It may be used in active processes or have dependencies. Please check and try again.';
                        }
                    }
                    
                    Swal.fire({
                        icon: 'error',
                        title: 'Cannot Delete Workflow',
                        text: errorMessage,
                        confirmButtonColor: '#dc2626'
                    });
                },
                complete: function() {
                    // This ensures loading stops regardless of success/error
                    console.log('Delete request completed');
                }
            });
        }
    });
}