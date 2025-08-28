// Approver Management Page JavaScript

$(document).ready(function() {
    // Initialize tooltips for action buttons
    $('[data-bs-toggle="tooltip"]').tooltip();
    
    // Handle delete approver button clicks first (before row clicks)
    $(document).on('click', '.delete-approver-btn', function(e) {
        e.preventDefault();
        e.stopPropagation();
        
        var approverId = $(this).data('approver-id');
        var approverName = $(this).data('approver-name');
        
        confirmDeleteApprover(approverId, approverName);
    });
    
    // Handle clickable rows (exclude clicks on action buttons)
    $('.clickable-row').on('click', function(e) {
        // Don't navigate if clicking on a button or its children
        if ($(e.target).closest('button').length > 0) {
            return;
        }
        
        var href = $(this).data('href');
        if (href) {
            window.location.href = href;
        }
    });
    
    // Auto-format approver name input
    $('#approverName').on('input', function() {
        var value = $(this).val().toUpperCase();
        $(this).val(value);
    });
    
    // Handle form submission
    $('#addApproverForm').on('submit', function(e) {
        e.preventDefault();
        
        // Collect roles data
        var roles = [];
        var hasError = false;
        var errorMessage = '';
        
        console.log('Role items found:', $('.role-item').length);
        
        $('.role-item').each(function(index) {
            var $row = $(this);
            var roleId = $row.find('select[name="roleIds[]"]').val();
            
            console.log('Role item', index + 1, 'roleId:', roleId);
            
            // Validate each role row (only visible ones)
            if ($row.is(':visible')) {
                if (!roleId || roleId === "" || roleId === "0") {
                    hasError = true;
                    errorMessage = `Please select a role for role ${index + 1}.`;
                    return false; // Stop the loop
                }
                
                var roleData = {
                    roleId: parseInt(roleId)
                };
                
                roles.push(roleData);
            }
        });
        
        console.log('Roles collected:', roles);
        
        if (hasError) {
            Swal.fire({
                icon: 'warning',
                title: 'Validation Error!',
                text: errorMessage,
                confirmButtonColor: '#dc2626'
            });
            return;
        }
        
        if (!roles || roles.length === 0) {
            Swal.fire({
                icon: 'warning',
                title: 'Warning!',
                text: 'Please add at least one role.',
                confirmButtonColor: '#dc2626'
            });
            return;
        }
        
        // Format approver name with APV_ prefix
        var approverNameInput = $('#approverName').val().trim().toUpperCase();
        
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
        
        // Remove APV_ prefix if user accidentally typed it
        if (approverNameInput.startsWith('APV_')) {
            approverNameInput = approverNameInput.substring(2);
        }
        
        var fullApproverName = 'APV_' + approverNameInput;
        
        // Send data in the format expected by CreateApproverViewModel
        var formData = {
            approverName: fullApproverName,
            isActive: $('#isActive').is(':checked'),
            rolesJson: JSON.stringify(roles)
        };
        
        // Show loading state
        var submitBtn = $(this).find('button[type="submit"]');
        var originalText = submitBtn.html();
        submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>Saving...').prop('disabled', true);
        
        $.ajax({
            url: window.approverManagementUrls?.create || '/ApproverManagement/Create',
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
            error: function(xhr, status, error) {
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
});

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
                        // Success - approver deleted
                        Swal.fire({
                            icon: 'success',
                            title: 'Deleted!',
                            text: `Approver "${approverName}" has been deleted successfully.`,
                            confirmButtonColor: '#3b82f6'
                        }).then(() => {
                            location.reload();
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

function addRole() {
    // Get existing select options
    var existingSelect = $('select[name="roleIds[]"]').first();
    var optionsHtml = existingSelect.html();
    
    var newRoleItem = $(`
        <div class="role-item mb-3">
            <label class="form-label text-muted">Role</label>
            <div class="d-flex gap-2">
                <select class="form-select form-select-enhanced flex-grow-1" name="roleIds[]" required>
                    ${optionsHtml}
                </select>
                <button type="button" class="btn btn-outline-danger" onclick="removeRole(this)" title="Remove Role">
                    <i class="bi bi-trash"></i>
                </button>
            </div>
        </div>
    `);
    
    // Reset the select value to empty
    newRoleItem.find('select').val('');
    
    $('#rolesContainer').append(newRoleItem);
}

function removeRole(button) {
    if ($('.role-item').length > 1) {
        $(button).closest('.role-item').remove();
    } else {
        Swal.fire({
            icon: 'warning',
            title: 'Warning!',
            text: 'At least one role is required.',
            confirmButtonColor: '#dc2626'
        });
    }
}