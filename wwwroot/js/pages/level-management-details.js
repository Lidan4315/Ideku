// Level Management Details Page JavaScript

$(document).ready(function() {
    // Handle add approver form submission
    $('#addApproverForm').on('submit', function(e) {
        e.preventDefault();
        
        var formData = {
            levelId: $('#levelId').val(),
            roleId: $('#roleId').val(),
            isPrimary: $('#isPrimary').is(':checked'),
            approvalLevel: $('#approvalLevel').val()
        };
        
        // Validate form
        if (!formData.roleId || !formData.approvalLevel) {
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
            url: window.levelManagementUrls?.addApprover || '/LevelManagement/AddApprover',
            type: 'POST',
            data: formData,
            success: function(response) {
                if (response.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Success!',
                        text: 'Approver added successfully.',
                        confirmButtonColor: '#3b82f6'
                    }).then(() => {
                        // Close modal and reload page
                        $('#addApproverModal').modal('hide');
                        location.reload();
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error!',
                        text: response.message || 'Failed to add approver.',
                        confirmButtonColor: '#dc2626'
                    });
                }
            },
            error: function() {
                Swal.fire({
                    icon: 'error',
                    title: 'Error!',
                    text: 'An error occurred while adding the approver.',
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
    $('#addApproverModal').on('hidden.bs.modal', function () {
        $('#addApproverForm')[0].reset();
    });
    
    // Auto-format level name input for edit form
    $('#editLevelName').on('input', function() {
        var value = $(this).val().toUpperCase();
        $(this).val(value);
    });
    
    // Handle edit level form submission
    $('#editLevelForm').on('submit', function(e) {
        e.preventDefault();
        
        // Format level name with LV prefix
        var levelNameInput = $('#editLevelName').val().trim().toUpperCase();
        
        // Validate level name input
        if (!levelNameInput) {
            Swal.fire({
                icon: 'warning',
                title: 'Validation Error!',
                text: 'Please enter a level identifier.',
                confirmButtonColor: '#dc2626'
            });
            return;
        }
        
        // Remove LV prefix if user accidentally typed it
        if (levelNameInput.startsWith('LV')) {
            levelNameInput = levelNameInput.substring(2);
        }
        
        var fullLevelName = 'LV' + levelNameInput;
        var levelId = $('#editLevelId').val();
        
        // Send data (only level name and active status)
        var formData = {
            id: parseInt(levelId),
            levelName: fullLevelName,
            isActive: $('#editIsActive').is(':checked')
        };
        
        // Show loading state
        var submitBtn = $(this).find('button[type="submit"]');
        var originalText = submitBtn.html();
        submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>Updating...').prop('disabled', true);
        
        $.ajax({
            url: (window.levelManagementUrls?.editBasic || '/LevelManagement/EditBasic') + '/' + levelId,
            type: 'POST',
            data: formData,
            success: function(response) {
                if (response.success) {
                    // Close modal
                    var editModal = bootstrap.Modal.getInstance(document.getElementById('editLevelModal'));
                    editModal.hide();
                    
                    Swal.fire({
                        icon: 'success',
                        title: 'Success!',
                        text: 'Level updated successfully.',
                        confirmButtonColor: '#3b82f6'
                    }).then(() => {
                        location.reload();
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error!',
                        text: response.message || 'Failed to update level.',
                        confirmButtonColor: '#dc2626'
                    });
                }
            },
            error: function(xhr, status, error) {
                Swal.fire({
                    icon: 'error',
                    title: 'Error!',
                    text: 'An error occurred while updating the level.',
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

// Delete approver function
function confirmDeleteApprover(approverId, roleName) {
    Swal.fire({
        title: 'Remove Approver?',
        text: `You are about to remove "${roleName}" as an approver for this level. This action cannot be undone!`,
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
                text: 'Please wait while we remove the approver.',
                allowOutsideClick: false,
                allowEscapeKey: false,
                showConfirmButton: false,
                willOpen: () => {
                    Swal.showLoading();
                }
            });
            
            $.ajax({
                url: window.levelManagementUrls?.deleteApprover || '/LevelManagement/DeleteApprover',
                type: 'POST',
                data: { levelApproverId: approverId },
                success: function(response) {
                    if (response.success) {
                        Swal.fire({
                            icon: 'success',
                            title: 'Deleted!',
                            text: 'Approver has been removed successfully.',
                            confirmButtonColor: '#3b82f6'
                        }).then(() => {
                            location.reload();
                        });
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: 'Error!',
                            text: response.message || 'Failed to remove approver.',
                            confirmButtonColor: '#dc2626'
                        });
                    }
                },
                error: function() {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error!',
                        text: 'An error occurred while removing the approver.',
                        confirmButtonColor: '#dc2626'
                    });
                }
            });
        }
    });
}

// Delete level function  
function confirmDeleteLevel(levelId, levelName) {
    Swal.fire({
        title: 'Are you sure?',
        text: `You are about to delete level "${levelName}". This action cannot be undone!`,
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
                text: 'Please wait while we delete the level.',
                allowOutsideClick: false,
                allowEscapeKey: false,
                showConfirmButton: false,
                willOpen: () => {
                    Swal.showLoading();
                }
            });
            
            // Perform AJAX delete action
            $.ajax({
                url: (window.levelManagementUrls?.delete || '/LevelManagement/Delete') + '/' + levelId,
                type: 'GET', // Controller expects GET request
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                success: function(response) {
                    // Check if response has error
                    if (response.success === false) {
                        Swal.fire({
                            icon: 'error',
                            title: 'Cannot Delete Level',
                            text: response.error,
                            confirmButtonColor: '#dc2626'
                        });
                    } else if (response.success === true) {
                        // Success - level deleted, redirect to index
                        Swal.fire({
                            icon: 'success',
                            title: 'Deleted!',
                            text: `Level "${levelName}" has been deleted successfully.`,
                            confirmButtonColor: '#3b82f6'
                        }).then(() => {
                            window.location.href = '/LevelManagement';
                        });
                    }
                },
                error: function(xhr) {
                    // Handle error response
                    var errorMessage = 'An error occurred while deleting the level.';
                    if (xhr.responseJSON && xhr.responseJSON.error) {
                        errorMessage = xhr.responseJSON.error;
                    } else if (xhr.responseText) {
                        // Check if response contains error message
                        if (xhr.responseText.includes('Cannot delete level')) {
                            errorMessage = 'Cannot delete level. It has assigned approvers or is used in workflow stages. Please remove them first.';
                        }
                    }
                    
                    Swal.fire({
                        icon: 'error',
                        title: 'Cannot Delete Level',
                        text: errorMessage,
                        confirmButtonColor: '#dc2626'
                    });
                }
            });
        }
    });
}

