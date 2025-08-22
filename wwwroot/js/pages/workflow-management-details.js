// Workflow Management Details Page JavaScript

$(document).ready(function() {

    // Add Stage Form Submit
    $('#addStageForm').on('submit', function(e) {
        e.preventDefault();
    
    const formData = {
        workflowId: document.getElementById('workflowId').value,
        levelId: document.getElementById('levelId').value,
        stage: parseInt(document.getElementById('stage').value),
        isMandatory: document.getElementById('isMandatory').checked,
        isParallel: document.getElementById('isParallel').checked
    };
    
    fetch(window.workflowManagementUrls?.addStage || '/WorkflowManagement/AddStage', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: new URLSearchParams(formData)
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
            Swal.fire({
                icon: 'success',
                title: 'Success!',
                text: 'Workflow stage added successfully.',
                confirmButtonColor: '#3b82f6'
            }).then(() => {
                bootstrap.Modal.getInstance(document.getElementById('addStageModal')).hide();
                location.reload();
            });
        } else {
            Swal.fire({
                icon: 'error',
                title: 'Error!',
                text: data.message,
                confirmButtonColor: '#ef4444'
            });
        }
    })
    .catch(error => {
        Swal.fire({
            icon: 'error',
            title: 'Error!',
            text: 'An error occurred while adding the stage.',
            confirmButtonColor: '#ef4444'
        });
    });
    });

    // Add Condition Form Submit  
    $('#addConditionForm').on('submit', function(e) {
        e.preventDefault();
        e.stopPropagation();
        
        // Get form values
        var workflowId = $('#conditionWorkflowId').val();
        var conditionType = $('#conditionType').val();
        var operator = $('#operatorType').val();
        var conditionValue = $('#conditionValue').is(':visible') ? $('#conditionValue').val() : $('#conditionSelect').val();
        
        // Validation
        if (!operator) {
            Swal.fire({
                icon: 'warning',
                title: 'Missing Operator',
                text: 'Please select an operator!',
                confirmButtonColor: '#f59e0b'
            });
            return;
        }
        
        if (!workflowId || !conditionType || !conditionValue) {
            Swal.fire({
                icon: 'warning',
                title: 'Missing Fields',
                text: 'Please fill in all required fields.',
                confirmButtonColor: '#f59e0b'
            });
            return;
        }
        
        // Send request using jQuery (working version)
        $.post('/WorkflowManagement/AddCondition', {
            WorkflowId: workflowId,
            ConditionType: conditionType,
            Operator: operator,
            ConditionValue: conditionValue,
            IsActive: true
        }).done(function(data) {
            if (data.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Success!',
                    text: 'Workflow condition added successfully.',
                    confirmButtonColor: '#3b82f6'
                }).then(() => {
                    $('#addConditionModal').modal('hide');
                    location.reload();
                });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Error!',
                    text: data.message,
                    confirmButtonColor: '#ef4444'
                });
            }
        }).fail(function(xhr) {
            Swal.fire({
                icon: 'error',
                title: 'Request Failed!',
                text: 'An error occurred while adding the condition.',
                confirmButtonColor: '#ef4444'
            });
        });
    });
    
    // Auto-format workflow name input for edit form
    $('#editWorkflowName').on('input', function() {
        var value = $(this).val().toUpperCase();
        $(this).val(value);
    });
    
    // Handle edit workflow form submission
    $('#editWorkflowForm').on('submit', function(e) {
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
        var workflowId = $('#editWorkflowId').val();
        
        // Send data (only workflow name, description, and active status)
        var formData = {
            id: parseInt(workflowId),
            workflowName: fullWorkflowName,
            desc: $('#editDesc').val(),
            isActive: $('#editIsActive').is(':checked')
        };
        
        // Show loading state
        var submitBtn = $(this).find('button[type="submit"]');
        var originalText = submitBtn.html();
        submitBtn.html('<i class="bi bi-hourglass-split me-2"></i>Updating...').prop('disabled', true);
        
        $.ajax({
            url: (window.workflowManagementUrls?.editWorkflow || '/WorkflowManagement/EditBasic') + '/' + workflowId,
            type: 'POST',
            data: formData,
            success: function(response) {
                if (response.success) {
                    // Close modal
                    var editModal = bootstrap.Modal.getInstance(document.getElementById('editWorkflowModal'));
                    editModal.hide();
                    
                    Swal.fire({
                        icon: 'success',
                        title: 'Success!',
                        text: 'Workflow updated successfully.',
                        confirmButtonColor: '#3b82f6'
                    }).then(() => {
                        location.reload();
                    });
                } else {
                    Swal.fire({
                        icon: 'error',
                        title: 'Error!',
                        text: response.message || 'Failed to update workflow.',
                        confirmButtonColor: '#dc2626'
                    });
                }
            },
            error: function(xhr, status, error) {
                Swal.fire({
                    icon: 'error',
                    title: 'Error!',
                    text: 'An error occurred while updating the workflow.',
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

// Delete Stage functions - outside document.ready since they're called from HTML onclick
function deleteStage(stageId) {
    Swal.fire({
        title: 'Are you sure?',
        text: 'You are about to delete this workflow stage. This action cannot be undone!',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef4444',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Yes, delete it!',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
        fetch(window.workflowManagementUrls?.deleteStage || '/WorkflowManagement/DeleteStage', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: new URLSearchParams({ stageId: stageId })
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Deleted!',
                    text: 'Workflow stage has been deleted successfully.',
                    confirmButtonColor: '#3b82f6'
                }).then(() => {
                    location.reload();
                });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Delete Failed!',
                    text: data.message,
                    confirmButtonColor: '#ef4444'
                });
            }
        })
        .catch(error => {
            Swal.fire({
                icon: 'error',
                title: 'Error!',
                text: 'An error occurred while deleting the stage.',
                confirmButtonColor: '#ef4444'
            });
        });
        }
    });
}

// Delete Condition
function deleteCondition(conditionId) {
    Swal.fire({
        title: 'Are you sure?',
        text: 'You are about to delete this workflow condition. This action cannot be undone!',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#ef4444',
        cancelButtonColor: '#6b7280',
        confirmButtonText: 'Yes, delete it!',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
        fetch(window.workflowManagementUrls?.deleteCondition || '/WorkflowManagement/DeleteCondition', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: new URLSearchParams({ conditionId: conditionId })
        })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Deleted!',
                    text: 'Workflow condition has been deleted successfully.',
                    confirmButtonColor: '#3b82f6'
                }).then(() => {
                    location.reload();
                });
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Delete Failed!',
                    text: data.message,
                    confirmButtonColor: '#ef4444'
                });
            }
        })
        .catch(error => {
            Swal.fire({
                icon: 'error',
                title: 'Error!',
                text: 'An error occurred while deleting the condition.',
                confirmButtonColor: '#ef4444'
            });
        });
        }
    });
}

// Update condition fields based on type
function updateConditionFields() {
    const conditionType = document.getElementById('conditionType').value;
    const conditionValueDiv = document.getElementById('conditionValueDiv');
    const conditionSelectDiv = document.getElementById('conditionSelectDiv');
    const conditionSelect = document.getElementById('conditionSelect');
    const conditionValueHelp = document.getElementById('conditionValueHelp');
    
    // Reset
    conditionValueDiv.style.display = 'block';
    conditionSelectDiv.style.display = 'none';
    conditionSelect.innerHTML = '<option value="">Select...</option>';
    
    // Load options based on condition type
    if (conditionType === 'CATEGORY') {
        conditionValueDiv.style.display = 'none';
        conditionSelectDiv.style.display = 'block';
        // Options will be populated by inline script in .cshtml
    } else if (conditionType === 'DIVISION') {
        conditionValueDiv.style.display = 'none';
        conditionSelectDiv.style.display = 'block';
        // Options will be populated by inline script in .cshtml
    } else if (conditionType === 'DEPARTMENT') {
        conditionValueDiv.style.display = 'none';
        conditionSelectDiv.style.display = 'block';
        // Options will be populated by inline script in .cshtml
    } else if (conditionType === 'SAVING_COST') {
        conditionValueHelp.textContent = 'Enter amount (e.g., 1000000 for 1 million)';
    } else if (conditionType === 'EVENT') {
        conditionValueHelp.textContent = 'Enter event type or description';
    }
}

// Open edit workflow modal function
function openEditWorkflowModal(id, workflowName, desc, isActive) {
    // Set basic data
    document.getElementById('editWorkflowId').value = id;
    
    // Remove WF_ prefix for display
    var displayName = workflowName.startsWith('WF_') ? workflowName.substring(3) : workflowName;
    document.getElementById('editWorkflowName').value = displayName;
    
    document.getElementById('editDesc').value = desc || '';
    document.getElementById('editIsActive').checked = isActive;
    
    // Show modal
    var editModal = new bootstrap.Modal(document.getElementById('editWorkflowModal'));
    editModal.show();
}

// Confirm delete workflow function
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
                url: (window.workflowManagementUrls?.deleteWorkflow || '/WorkflowManagement/Delete') + '/' + workflowId,
                type: 'GET',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                success: function(response) {
                    if (response.success === false) {
                        Swal.fire({
                            icon: 'error',
                            title: 'Cannot Delete Workflow',
                            text: response.error,
                            confirmButtonColor: '#dc2626'
                        });
                    } else if (response.success === true) {
                        // Success - workflow deleted, redirect to index
                        Swal.fire({
                            icon: 'success',
                            title: 'Deleted!',
                            text: `Workflow "${workflowName}" has been deleted successfully.`,
                            confirmButtonColor: '#3b82f6'
                        }).then(() => {
                            window.location.href = '/WorkflowManagement';
                        });
                    }
                },
                error: function(xhr) {
                    var errorMessage = 'An error occurred while deleting the workflow.';
                    if (xhr.responseJSON && xhr.responseJSON.error) {
                        errorMessage = xhr.responseJSON.error;
                    }
                    
                    Swal.fire({
                        icon: 'error',
                        title: 'Cannot Delete Workflow',
                        text: errorMessage,
                        confirmButtonColor: '#dc2626'
                    });
                }
            });
        }
    });
}