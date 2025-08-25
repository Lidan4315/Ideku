// Level Management Page JavaScript

$(document).ready(function() {
    // Initialize tooltips for action buttons
    $('[data-bs-toggle="tooltip"]').tooltip();
    
    // Handle delete level button clicks first (before row clicks)
    $(document).on('click', '.delete-level-btn', function(e) {
        e.preventDefault();
        e.stopPropagation();
        
        var levelId = $(this).data('level-id');
        var levelName = $(this).data('level-name');
        
        confirmDeleteLevel(levelId, levelName);
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
    
    // Auto-format level name input
    $('#levelName').on('input', function() {
        var value = $(this).val().toUpperCase();
        $(this).val(value);
    });
    
    // Handle form submission
    $('#addLevelForm').on('submit', function(e) {
        e.preventDefault();
        
        // Collect approvers data
        var approvers = [];
        var hasError = false;
        var errorMessage = '';
        
        
        $('.approver-card').each(function(index) {
            var $row = $(this);
            var roleId = $row.find('select[name="roleIds[]"]').val();
            var approvalLevel = $row.find('input[name="approvalLevels[]"]').val();
            var isPrimary = $row.find('input[name="isPrimaries[]"]').is(':checked');
            
            
            // Validate each approver row (only visible ones)
            if ($row.is(':visible')) {
                if (!roleId || roleId === "" || roleId === "0") {
                    hasError = true;
                    errorMessage = `Please select a role for approver ${index + 1}.`;
                        return false; // Stop the loop
                }
                
                if (!approvalLevel || approvalLevel === "" || parseInt(approvalLevel) <= 0) {
                    hasError = true;
                    errorMessage = `Please enter a valid approval level for approver ${index + 1}.`;
                        return false; // Stop the loop
                }
                
                var approverData = {
                    roleId: parseInt(roleId),
                    approvalLevel: parseInt(approvalLevel),
                    isPrimary: isPrimary
                };
                
                approvers.push(approverData);
            }
        });
        
        if (hasError) {
            Swal.fire({
                icon: 'warning',
                title: 'Validation Error!',
                text: errorMessage,
                confirmButtonColor: '#dc2626'
            });
            return;
        }
        
        
        if (hasError) {
            Swal.fire({
                icon: 'warning',
                title: 'Validation Error!',
                text: errorMessage,
                confirmButtonColor: '#dc2626'
            });
            return;
        }
        
        if (!approvers || approvers.length === 0) {
            Swal.fire({
                icon: 'warning',
                title: 'Warning!',
                text: 'Please add at least one approver.',
                confirmButtonColor: '#dc2626'
            });
            return;
        }
        
        // Format level name with LV prefix
        var levelNameInput = $('#levelName').val().trim().toUpperCase();
        
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
        
        // Send data in the format expected by CreateLevelViewModel
        var formData = {
            levelName: fullLevelName,
            isActive: $('#isActive').is(':checked'),
            approversJson: JSON.stringify(approvers) // Controller expects 'ApproversJson', not 'approvers'
        };
        
        
        // Show loading state
        var submitBtn = $(this).find('button[type="submit"]');
        var originalText = submitBtn.html();
        submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>Saving...').prop('disabled', true);
        
        $.ajax({
            url: window.levelManagementUrls?.create || '/LevelManagement/Create',
            type: 'POST',
            data: formData,
            success: function(response) {
                if (response.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Success!',
                        text: 'Level added successfully.',
                        confirmButtonColor: '#3b82f6'
                    }).then(() => {
                        location.reload();
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error!',
                        text: response.message || 'Failed to add level.',
                        confirmButtonColor: '#dc2626'
                    });
                }
            },
            error: function(xhr, status, error) {
                Swal.fire({
                    icon: 'error',
                    title: 'Error!',
                    text: 'An error occurred while adding the level.',
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
                        // Success - level deleted
                        Swal.fire({
                            icon: 'success',
                            title: 'Deleted!',
                            text: `Level "${levelName}" has been deleted successfully.`,
                            confirmButtonColor: '#3b82f6'
                        }).then(() => {
                            location.reload();
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

function addApprover() {
    // Clone the existing select options instead of rebuilding from ViewBag
    var existingSelect = $('select[name="roleIds[]"]').first();
    var clonedSelect = existingSelect.clone();
    clonedSelect.val(''); // Reset selection
    
    var newApproverCard = $(`
        <div class="approver-card">
            <div class="row g-3">
                <div class="col-md-4">
                    <label class="form-label-sm">Role</label>
                    <!-- Placeholder for select -->
                </div>
                <div class="col-md-3">
                    <label class="form-label-sm">Level</label>
                    <input type="number" class="form-control" name="approvalLevels[]" 
                           placeholder="Level" min="1" required>
                </div>
                <div class="col-md-3">
                    <label class="form-label-sm">Options</label>
                    <div class="d-flex align-items-center gap-2">
                        <div class="form-check">
                            <input class="form-check-input" type="checkbox" name="isPrimaries[]" value="1">
                            <label class="form-check-label">Primary</label>
                        </div>
                    </div>
                </div>
                <div class="col-md-2">
                    <label class="form-label-sm">&nbsp;</label>
                    <div>
                        <button type="button" class="btn btn-outline-danger btn-sm" onclick="removeApprover(this)">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `);
    
    // Replace placeholder with cloned select and add proper classes
    clonedSelect.addClass('form-select');
    newApproverCard.find('.col-md-4').append(clonedSelect);
    
    $('#approversContainer').append(newApproverCard);
}

function removeApprover(button) {
    if ($('.approver-card').length > 1) {
        $(button).closest('.approver-card').remove();
    } else {
        Swal.fire({
            icon: 'warning',
            title: 'Warning!',
            text: 'At least one approver is required.',
            confirmButtonColor: '#dc2626'
        });
    }
}

