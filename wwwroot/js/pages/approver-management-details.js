// Approver Management Details Page JavaScript

$(document).ready(function() {
    // Handle add approver role form submission
    $('#addApproverRoleForm').on('submit', function(e) {
        e.preventDefault();
        
        var formData = {
            approverId: $('#approverId').val(),
            roleId: $('#roleId').val()
        };
        
        // Validate form
        if (!formData.roleId) {
            Swal.fire({
                icon: 'error',
                title: 'Validation Error',
                text: 'Please fill in all required fields.',
                confirmButtonColor: '#dc2626'
            });
            return;
        }
        
        // Show loading state
        var submitBtn = $(this).find('button[type="submit"]');
        var originalText = submitBtn.html();
        submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>Adding...').prop('disabled', true);
        
        $.ajax({
            url: window.approverManagementUrls?.addApproverRole || '/ApproverManagement/AddApproverRole',
            type: 'POST',
            data: formData,
            success: function(response) {
                if (response.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Success!',
                        text: 'Role added successfully.',
                        confirmButtonColor: '#3b82f6'
                    }).then(() => {
                        // Close modal and reload page
                        $('#addApproverRoleModal').modal('hide');
                        location.reload();
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error!',
                        text: response.message || 'Failed to add role.',
                        confirmButtonColor: '#dc2626'
                    });
                }
            },
            error: function() {
                Swal.fire({
                    icon: 'error',
                    title: 'Error!',
                    text: 'An error occurred while adding the role.',
                    confirmButtonColor: '#dc2626'
                });
            },
            complete: function() {
                // Restore button state
                submitBtn.html(originalText).prop('disabled', false);
            }
        });
    });

    // Reset form when modal is hidden
    $('#addApproverRoleModal').on('hidden.bs.modal', function () {
        $('#addApproverRoleForm')[0].reset();
    });
    
    // Auto-format approver name input for edit form
    $('#editApproverName').on('input', function() {
        var value = $(this).val().toUpperCase();
        $(this).val(value);
    });
    
    // Handle edit approver form submission
    $('#editApproverForm').on('submit', function(e) {
        e.preventDefault();
        
        // Format approver name with LV prefix
        var approverNameInput = $('#editApproverName').val().trim().toUpperCase();
        
        // Validate approver name input
        if (!approverNameInput) {
            Swal.fire({
                icon: 'warning',
                title: 'Validation Error!',
                text: 'Please enter an approver identifier.',
                confirmButtonColor: '#dc2626'
            });
            return;
        }
        
        // Remove LV prefix if user accidentally typed it
        if (approverNameInput.startsWith('LV')) {
            approverNameInput = approverNameInput.substring(2);
        }
        
        var fullApproverName = 'LV' + approverNameInput;
        var approverId = $('#editApproverId').val();
        
        // Send data (only approver name and active status)
        var formData = {
            id: parseInt(approverId),
            approverName: fullApproverName,
            isActive: $('#editIsActive').is(':checked')
        };
        
        // Show loading state
        var submitBtn = $(this).find('button[type="submit"]');
        var originalText = submitBtn.html();
        submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>Updating...').prop('disabled', true);
        
        $.ajax({
            url: (window.approverManagementUrls?.editBasic || '/ApproverManagement/EditBasic') + '/' + approverId,
            type: 'POST',
            data: formData,
            success: function(response) {
                if (response.success) {
                    // Close modal
                    var editModal = bootstrap.Modal.getInstance(document.getElementById('editApproverModal'));
                    editModal.hide();
                    
                    Swal.fire({
                        icon: 'success',
                        title: 'Success!',
                        text: 'Approver updated successfully.',
                        confirmButtonColor: '#3b82f6'
                    }).then(() => {
                        location.reload();
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error!',
                        text: response.message || 'Failed to update approver.',
                        confirmButtonColor: '#dc2626'
                    });
                }
            },
            error: function(xhr, status, error) {
                Swal.fire({
                    icon: 'error',
                    title: 'Error!',
                    text: 'An error occurred while updating the approver.',
                    confirmButtonColor: '#dc2626'
                });
            },
            complete: function() {
                // Restore button state
                submitBtn.html(originalText).prop('disabled', false);
            }
        });
    });
});

// Delete approver role function
function confirmDeleteApproverRole(approverRoleId, roleName) {
    Swal.fire({
        title: 'Remove Role?',
        text: `You are about to remove "${roleName}" as a role for this approver. This action cannot be undone!`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, remove it!'
    }).then((result) => {
        if (result.isConfirmed) {
            // Show loading
            Swal.fire({
                title: 'Removing...',
                text: 'Please wait while we remove the role.',
                allowOutsideClick: false,
                allowEscapeKey: false,
                showConfirmButton: false,
                willOpen: () => {
                    Swal.showLoading();
                }
            });
            
            $.ajax({
                url: window.approverManagementUrls?.deleteApproverRole || '/ApproverManagement/DeleteApproverRole',
                type: 'POST',
                data: { approverRoleId: approverRoleId },
                success: function(response) {
                    if (response.success) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Deleted!',
                            text: 'Role has been removed successfully.',
                            confirmButtonColor: '#3b82f6'
                        }).then(() => {
                            location.reload();
                        });
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: 'Error!',
                            text: response.message || 'Failed to remove role.',
                            confirmButtonColor: '#dc2626'
                        });
                    }
                },
                error: function() {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error!',
                        text: 'An error occurred while removing the role.',
                        confirmButtonColor: '#dc2626'
                    });
                }
            });
        }
    });
}

// Delete approver function  
function confirmDeleteApprover(approverId, approverName) {
    Swal.fire({
        title: 'Are you sure?',
        text: `You are about to delete approver "${approverName}". This action cannot be undone!`,
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
                text: 'Please wait while we delete the approver.',
                allowOutsideClick: false,
                allowEscapeKey: false,
                showConfirmButton: false,
                willOpen: () => {
                    Swal.showLoading();
                }
            });
            
            // Perform AJAX delete action
            $.ajax({
                url: (window.approverManagementUrls?.delete || '/ApproverManagement/Delete') + '/' + approverId,
                type: 'GET', // Controller expects GET request
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                success: function(response) {
                    // Check if response has error
                    if (response.success === false) {
                        Swal.fire({
                            icon: 'error',
                            title: 'Cannot Delete Approver',
                            text: response.error,
                            confirmButtonColor: '#dc2626'
                        });
                    } else if (response.success === true) {
                        // Success - approver deleted, redirect to index
                        Swal.fire({
                            icon: 'success',
                            title: 'Deleted!',
                            text: `Approver "${approverName}" has been deleted successfully.`,
                            confirmButtonColor: '#3b82f6'
                        }).then(() => {
                            window.location.href = '/ApproverManagement';
                        });
                    }
                },
                error: function(xhr) {
                    // Handle error response
                    var errorMessage = 'An error occurred while deleting the approver.';
                    if (xhr.responseJSON && xhr.responseJSON.error) {
                        errorMessage = xhr.responseJSON.error;
                    } else if (xhr.responseText) {
                        // Check if response contains error message
                        if (xhr.responseText.includes('Cannot delete approver')) {
                            errorMessage = 'Cannot delete approver. It has assigned roles or is used in workflow stages. Please remove them first.';
                        }
                    }
                    
                    Swal.fire({
                        icon: 'error',
                        title: 'Cannot Delete Approver',
                        text: errorMessage,
                        confirmButtonColor: '#dc2626'
                    });
                }
            });
        }
    });
}