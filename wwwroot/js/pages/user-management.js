// User Management JavaScript
// Handles AJAX operations, form submissions, and UI interactions
// Following the same pattern as role-management.js for consistency

// ============================================
// UTILITY FUNCTIONS
// ============================================

/**
 * Simple toast notification function
 * @param {string} message - Message to display
 * @param {string} type - Type: 'success', 'error', 'warning', 'info'
 */
function showToast(message, type = 'info') {
    // Silent operation - no annoying alerts
    // TODO: Implement proper toast notification library if needed
}

$(document).ready(function() {
    initializeUserManagement();
});

function initializeUserManagement() {
    // Initialize form submissions
    initializeCreateUserForm();
    initializeEditUserForm();

    // Initialize button event handlers
    initializeEditButtons();
    initializeDeleteButtons();

    // Initialize employee validation
    initializeEmployeeValidation();

    // Initialize acting management functionality
    initializeActingManagement();

}

// ============================================
// CREATE USER FUNCTIONALITY
// ============================================

function initializeCreateUserForm() {
    $('#addUserForm').on('submit', function(e) {
        e.preventDefault();
        
        const form = $(this);
        
        // Get form data for confirmation
        const employeeId = $('#employeeId').val();
        const employeeName = $('#employeeName').val() || 'Unknown Employee';
        const username = $('#username').val().trim();
        const roleId = $('#roleId').val();
        const roleName = $('#roleId option:selected').text();
        const isActing = $('#isActing').is(':checked');
        
        // Validate employee first
        if (!validateEmployeeSelection()) {
            showToast('Please enter valid employee ID first.', 'error');
            return;
        }
        
        // Show confirmation dialog before saving
        Swal.fire({
            title: 'Create User Account',
            html: `Are you sure you want to create a user account for:<br><br>
                   <strong>Employee:</strong> ${employeeName}<br>
                   <strong>Username:</strong> ${username}<br>
                   <strong>Role:</strong> ${roleName}<br>
                   <strong>Acting:</strong> ${isActing ? 'Yes' : 'No'}<br><br>
                   <small class="text-muted">This will create a new user account in the system.</small>`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#198754',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, Create User',
            cancelButtonText: 'Cancel',
            reverseButtons: true
        }).then((result) => {
            if (result.isConfirmed) {
                saveNewUser(form);
            }
        });
    });
}

function saveNewUser(form) {
    // Show loading state
    Swal.fire({
        title: 'Creating User...',
        text: 'Please wait while we create the user account.',
        icon: 'info',
        allowOutsideClick: false,
        showConfirmButton: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
    
    // Get form data
    const formData = {
        employeeId: $('#employeeId').val(),
        username: $('#username').val().trim(),
        roleId: parseInt($('#roleId').val()),
        isActing: $('#isActing').is(':checked')
    };
    
    // Add anti-forgery token
    const token = form.find('input[name="__RequestVerificationToken"]').val();
    
    // AJAX request to create user
    $.ajax({
        url: window.userManagementUrls.create,
        type: 'POST',
        data: formData,
        headers: {
            'RequestVerificationToken': token
        },
        success: function(response) {
            if (response.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'User Created!',
                    text: response.message || 'User account created successfully.',
                    confirmButtonColor: '#198754',
                    timer: 2000,
                    showConfirmButton: false
                });
                
                // Close modal and reload page after short delay
                $('#addUserModal').modal('hide');
                setTimeout(() => {
                    window.location.reload();
                }, 2000);
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Cannot Create User',
                    text: response.message,
                    confirmButtonColor: '#dc2626'
                });
            }
        },
        error: function(xhr, status, error) {
            console.error('Create user error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'An error occurred while creating the user.',
                confirmButtonColor: '#dc2626'
            });
        }
    });
}

// ============================================
// EDIT USER FUNCTIONALITY
// ============================================

function initializeEditButtons() {
    $(document).on('click', '.edit-user-btn', function(e) {
        e.preventDefault();
        e.stopPropagation();

        const userId = $(this).data('user-id');
        loadUserForEdit(userId);
    });
}

function loadUserForEdit(userId) {
    // Show loading in edit modal
    showEditModalLoading();
    
    $.ajax({
        url: window.userManagementUrls.getUser,
        type: 'GET',
        data: { id: userId },
        success: function(response) {
            if (response.success) {
                populateEditModal(response.user);
                $('#editUserModal').modal('show');
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Error!',
                    text: response.message,
                    confirmButtonColor: '#dc2626'
                });
            }
        },
        error: function(xhr, status, error) {
            console.error('Load user error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Error!',
                text: 'Error loading user information.',
                confirmButtonColor: '#dc2626'
            });
        }
    });
}

function showEditModalLoading() {
    $('#editUserId').val('');
    $('#editUsername').val('Loading...');
    $('#editRoleId').val('');
    $('#editIsActing').prop('checked', false);
    $('#editUserWarning').addClass('d-none');
    
    // Clear employee info
    $('#editEmployeeName').text('Loading...');
    $('#editEmployeeId').text('Loading...');
    $('#editEmployeePosition').text('Loading...');
    $('#editEmployeeEmail').text('Loading...');
    $('#editDivisionName').text('Loading...');
    $('#editDepartmentName').text('Loading...');
}

function populateEditModal(user) {
    // Populate form fields
    $('#editUserId').val(user.id);
    $('#editUsername').val(user.username);
    $('#editRoleId').val(user.roleId);
    $('#editIsActing').prop('checked', user.isActing);

    // Populate employee information (read-only)
    $('#editEmployeeName').text(user.employeeName);
    $('#editEmployeeId').text(user.employeeId);
    $('#editEmployeePosition').text(user.employeePosition);
    $('#editEmployeeEmail').text(user.employeeEmail);
    $('#editDivisionName').text(user.divisionName);
    $('#editDepartmentName').text(user.departmentName);

    // FRONTEND PROTECTION: Check if user is currently acting
    if (user.isCurrentlyActing) {
        // Disable role field
        $('#editRoleId').prop('disabled', true);

        // Show acting protection warning
        $('#editUserWarningText').html(
            '<i class="bi bi-exclamation-triangle text-warning me-2"></i>' +
            '<strong>Role Protected:</strong> Cannot change role while user is acting. ' +
            'Stop acting first or wait until acting period expires.'
        );
        $('#editUserWarning').removeClass('d-none');
    } else {
        // Enable role field
        $('#editRoleId').prop('disabled', false);

        // Show dependency warning if exists
        if (user.dependencyCount > 0) {
            $('#editUserWarningText').text(`Warning: This user has ${user.dependencyCount} associated record(s). Changes may affect system data.`);
            $('#editUserWarning').removeClass('d-none');
        } else {
            $('#editUserWarning').addClass('d-none');
        }
    }

    // Update modal title
    $('#editUserModalLabel').text(`Edit User: "${user.username}"`);
}

function initializeEditUserForm() {
    $('#editUserForm').on('submit', function(e) {
        e.preventDefault();
        
        const form = $(this);
        
        // Get form data for confirmation
        const userId = $('#editUserId').val();
        const username = $('#editUsername').val().trim();
        const roleId = $('#editRoleId').val();
        const roleName = $('#editRoleId option:selected').text();
        const isActing = $('#editIsActing').is(':checked');
        
        // Show confirmation dialog before updating
        Swal.fire({
            title: 'Update User Account',
            html: `Are you sure you want to update this user account?<br><br>
                   <strong>Username:</strong> ${username}<br>
                   <strong>Role:</strong> ${roleName}<br>
                   <strong>Acting:</strong> ${isActing ? 'Yes' : 'No'}<br><br>
                   <small class="text-muted">This will modify the user account information.</small>`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#198754',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, Update User',
            cancelButtonText: 'Cancel',
            reverseButtons: true
        }).then((result) => {
            if (result.isConfirmed) {
                updateExistingUser(form, userId);
            }
        });
    });
}

function updateExistingUser(form, userId) {
    // Show loading state
    Swal.fire({
        title: 'Updating User...',
        text: 'Please wait while we update the user account.',
        icon: 'info',
        allowOutsideClick: false,
        showConfirmButton: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
    
    // Get form data
    const formData = {
        id: userId,
        username: $('#editUsername').val().trim(),
        roleId: parseInt($('#editRoleId').val()),
        isActing: $('#editIsActing').is(':checked')
    };
    
    // Add anti-forgery token
    const token = form.find('input[name="__RequestVerificationToken"]').val();
    
    // AJAX request to update user
    $.ajax({
        url: window.userManagementUrls.edit + '/' + userId,
        type: 'POST',
        data: formData,
        headers: {
            'RequestVerificationToken': token
        },
        success: function(response) {
            if (response.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'User Updated!',
                    text: response.message || 'User account updated successfully.',
                    confirmButtonColor: '#198754',
                    timer: 2000,
                    showConfirmButton: false
                });
                
                // Close modal and reload page after short delay
                $('#editUserModal').modal('hide');
                setTimeout(() => {
                    window.location.reload();
                }, 2000);
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Cannot Update User',
                    text: response.message,
                    confirmButtonColor: '#dc2626'
                });
            }
        },
        error: function(xhr, status, error) {
            console.error('Update user error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'An error occurred while updating the user.',
                confirmButtonColor: '#dc2626'
            });
        }
    });
}

// ============================================
// DELETE USER FUNCTIONALITY
// ============================================

function initializeDeleteButtons() {
    $(document).on('click', '.delete-user-btn', function(e) {
        e.preventDefault();
        e.stopPropagation();
        
        const userId = $(this).data('user-id');
        const userName = $(this).data('user-name');
        
        confirmDeleteUser(userId, userName);
    });
}

function confirmDeleteUser(userId, userName) {
    Swal.fire({
        title: 'Delete User Account',
        html: `Are you sure you want to delete the user account for "<strong>${userName}</strong>"?<br><br>
               <small class="text-muted">This action cannot be undone. The system will check for dependencies before deletion.</small>`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#dc3545',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, Delete',
        cancelButtonText: 'Cancel',
        reverseButtons: true
    }).then((result) => {
        if (result.isConfirmed) {
            deleteUser(userId, userName);
        }
    });
}

function deleteUser(userId, userName) {
    // Show loading state
    Swal.fire({
        title: 'Deleting User...',
        text: 'Please wait while we delete the user account.',
        icon: 'info',
        allowOutsideClick: false,
        showConfirmButton: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
    
    $.ajax({
        url: window.userManagementUrls.delete,
        type: 'POST',
        data: { id: userId },
        success: function(response) {
            if (response.success) {
                Swal.fire({
                    title: 'User Deleted!',
                    text: response.message,
                    icon: 'success',
                    timer: 2000,
                    showConfirmButton: false
                });
                
                // Reload page after short delay
                setTimeout(() => {
                    window.location.reload();
                }, 2000);
            } else {
                Swal.fire({
                    title: 'Cannot Delete User',
                    text: response.message,
                    icon: 'error',
                    confirmButtonText: 'OK'
                });
            }
        },
        error: function(xhr, status, error) {
            console.error('Delete user error:', error);
            Swal.fire({
                title: 'Error',
                text: 'An error occurred while deleting the user.',
                icon: 'error',
                confirmButtonText: 'OK'
            });
        }
    });
}

// ============================================
// USER DETAILS FUNCTIONALITY
// ============================================


// ============================================
// FORM VALIDATION ENHANCEMENTS
// ============================================

// Real-time validation for username
$(document).on('input', '#username, #editUsername', function() {
    const input = $(this);
    const value = input.val().trim();
    
    // Clear previous validation
    input.removeClass('is-invalid is-valid');
    input.siblings('.invalid-feedback').remove();
    
    if (value.length === 0) {
        input.addClass('is-invalid');
        input.after('<div class="invalid-feedback">Username is required.</div>');
    } else if (value.length < 3) {
        input.addClass('is-invalid');
        input.after('<div class="invalid-feedback">Username must be at least 3 characters.</div>');
    } else if (value.length > 100) {
        input.addClass('is-invalid');
        input.after('<div class="invalid-feedback">Username cannot exceed 100 characters.</div>');
    } else if (!/^[a-zA-Z0-9._-]+$/.test(value)) {
        input.addClass('is-invalid');
        input.after('<div class="invalid-feedback">Username can only contain letters, numbers, dots, hyphens, and underscores.</div>');
    }
});

// Employee ID validation with auto-populate
let validateTimeout;
$(document).on('input', '#employeeId', function() {
    const input = $(this);
    const value = input.val().trim().toUpperCase();
    
    // Update input with uppercase
    input.val(value);
    
    // Clear previous timeout
    clearTimeout(validateTimeout);
    
    // Clear employee fields if input is too short
    if (value.length < 3) {
        clearEmployeeFields();
        input.removeClass('is-valid is-invalid');
        return;
    }
    
    // Auto-validate after 500ms delay (debounce)
    validateTimeout = setTimeout(() => {
        validateEmployeeAuto(value);
    }, 500);
});

// Role selection validation
$(document).on('change', '#roleId, #editRoleId', function() {
    const input = $(this);
    const value = input.val();
    
    input.removeClass('is-invalid is-valid');
    input.siblings('.invalid-feedback').remove();
    
    if (!value) {
        input.addClass('is-invalid');
        input.after('<div class="invalid-feedback">Please select a role.</div>');
    }
});

// ============================================
// EMPLOYEE VALIDATION FUNCTIONALITY
// ============================================

function initializeEmployeeValidation() {
    // Auto-validation is handled by input event
    console.log('Employee auto-validation initialized');
}

function validateEmployeeAuto(employeeId) {
    if (!employeeId || employeeId.length < 3) {
        return;
    }
    
    $.ajax({
        url: '/UserManagement/ValidateEmployee',
        type: 'GET',
        data: { employeeId: employeeId },
        success: function(response) {
            if (response.success) {
                populateEmployeeFields(response.employee);
            } else {
                showEmployeeValidationError(response.message);
            }
        },
        error: function(xhr, status, error) {
            console.error('Employee validation error:', error);
            showEmployeeValidationError('Error connecting to server.');
        }
    });
}

function populateEmployeeFields(employee) {
    // Populate form fields (keep readonly gray background)
    $('#employeeName').val(employee.name).removeClass('not-found-field');
    $('#employeePosition').val(employee.position).removeClass('not-found-field');
    $('#employeeDivision').val(employee.division).removeClass('not-found-field');
    $('#employeeDepartment').val(employee.department).removeClass('not-found-field');
    $('#employeeEmail').val(employee.email).removeClass('not-found-field');
    
    // Mark Employee ID as validated (without visual styling)
    $('#employeeId').removeClass('is-invalid is-valid').data('validated', true);
    
    // Store employee data for form submission
    $('#employeeId').data('employeeName', employee.name);
}

function showEmployeeValidationError(message) {
    showNotFoundFields();
    $('#employeeId').removeClass('is-valid is-invalid').data('validated', false);
    
    // Show toast notification for error
    showToast(message, 'error');
}

function clearEmployeeFields() {
    $('#employeeName, #employeePosition, #employeeDivision, #employeeDepartment, #employeeEmail').val('').removeClass('not-found-field');
    $('#employeeId').removeData('validated').removeData('employeeName');
}

function showNotFoundFields() {
    $('#employeeName, #employeePosition, #employeeDivision, #employeeDepartment, #employeeEmail')
        .val('Not Found')
        .addClass('not-found-field');
    $('#employeeId').removeData('validated').removeData('employeeName');
}

function validateEmployeeSelection() {
    const employeeId = $('#employeeId').val().trim();
    const isValidated = $('#employeeId').data('validated');
    const employeeName = $('#employeeName').val().trim();
    
    return employeeId && isValidated && employeeName;
}

// Clear validation when modals are closed
$('#addUserModal, #editUserModal').on('hidden.bs.modal', function() {
    const modal = $(this);
    modal.find('input, select').removeClass('is-invalid is-valid');
    modal.find('.invalid-feedback').remove();
    modal.find('form')[0].reset();
    
    // Clear employee fields and validation state
    clearEmployeeFields();
    clearTimeout(validateTimeout);
    
    // Hide acting badges
    $('#actingBadge, #editActingBadge').hide();
});

// ============================================
// ACTING POSITION BADGE FUNCTIONALITY
// ============================================

function initializeActingBadge() {
    // Handle acting badge visibility for Add User form
    $('#isActing').on('change', function() {
        const badge = $('#actingBadge');
        if (this.checked) {
            badge.fadeIn(200);
        } else {
            badge.fadeOut(200);
        }
    });
    
    // Handle acting badge visibility for Edit User form
    $('#editIsActing').on('change', function() {
        const badge = $('#editActingBadge');
        if (this.checked) {
            badge.fadeIn(200);
        } else {
            badge.fadeOut(200);
        }
    });
}

// Initialize acting badge functionality
$(document).ready(function() {
    initializeActingBadge();
});


// ============================================
// ACTING MANAGEMENT FUNCTIONALITY
// ============================================

function initializeActingManagement() {
    // Set Acting button handler
    $(document).on('click', '.set-acting-btn', function() {
        const userId = $(this).data('user-id');
        const userName = $(this).data('user-name');
        const currentRole = $(this).data('current-role');
        const currentDivision = $(this).data('current-division');
        const currentDepartment = $(this).data('current-department');

        openSetActingModal(userId, userName, currentRole, currentDivision, currentDepartment);
    });

    // Stop Acting button handler
    $(document).on('click', '.stop-acting-btn', function() {
        const userId = $(this).data('user-id');
        const userName = $(this).data('user-name');

        confirmStopActing(userId, userName);
    });

    // Extend Acting button handler
    $(document).on('click', '.extend-acting-btn', function() {
        const userId = $(this).data('user-id');
        const userName = $(this).data('user-name');
        const currentEndDate = $(this).data('current-end-date');

        openExtendActingModal(userId, userName, currentEndDate);
    });

    // Set Acting form submission
    $('#setActingForm').on('submit', function(e) {
        e.preventDefault();
        submitSetActing();
    });

    // Extend Acting form submission
    $('#extendActingForm').on('submit', function(e) {
        e.preventDefault();
        submitExtendActing();
    });

    // Initialize acting location functionality
    initializeActingLocation();
}

function openSetActingModal(userId, userName, currentRole, currentDivision, currentDepartment) {
    $('#setActingUserId').val(userId);
    $('#setActingUserName').text(userName);
    $('#setActingCurrentRole').text(currentRole);

    // Populate current location info
    $('#currentUserDivision').text(currentDivision || 'N/A');
    $('#currentUserDepartment').text(currentDepartment || 'N/A');

    // Reset form
    $('#setActingForm')[0].reset();
    $('#setActingUserId').val(userId); // Re-set after reset

    // Set default dates
    const today = new Date().toISOString().split('T')[0];
    const thirtyDaysLater = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];

    $('#setActingStartDate').val(today);
    $('#setActingEndDate').val(thirtyDaysLater);

    $('#setActingModal').modal('show');
}

function openExtendActingModal(userId, userName, currentEndDate) {
    $('#extendActingUserId').val(userId);
    $('#extendActingUserName').text(userName);
    $('#extendActingCurrentEnd').text(new Date(currentEndDate).toLocaleDateString());
    $('#extendActingCurrentEndDate').val(currentEndDate);

    // Set minimum date to current end date + 1 day
    const minDate = new Date(currentEndDate);
    minDate.setDate(minDate.getDate() + 1);
    $('#extendActingNewEndDate').attr('min', minDate.toISOString().split('T')[0]);

    // Set default to 30 days from current end date
    const defaultDate = new Date(currentEndDate);
    defaultDate.setDate(defaultDate.getDate() + 30);
    $('#extendActingNewEndDate').val(defaultDate.toISOString().split('T')[0]);

    $('#extendActingModal').modal('show');
}

function confirmStopActing(userId, userName) {
    Swal.fire({
        title: 'Stop Acting Role',
        text: `Are you sure you want to stop acting role for ${userName}? This will revert them to their original role immediately.`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, Stop Acting',
        cancelButtonText: 'Cancel'
    }).then((result) => {
        if (result.isConfirmed) {
            submitStopActing(userId);
        }
    });
}

function submitSetActing() {
    // Get acting location data
    const locationData = getActingLocationData();

    const formData = {
        userId: parseInt($('#setActingUserId').val()),
        actingRoleId: parseInt($('#setActingRoleSelect').val()),
        actingStartDate: $('#setActingStartDate').val(),
        actingEndDate: $('#setActingEndDate').val(),
        actingDivisionId: locationData.actingDivisionId,
        actingDepartmentId: locationData.actingDepartmentId
    };

    // Validate dates
    if (!validateSetActingForm(formData)) {
        return;
    }

    $.ajax({
        url: window.userManagementUrls.setActing,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function(response) {
            if (response.success) {
                $('#setActingModal').modal('hide');
                showToast(response.message, 'success');
                refreshUserTable();
            } else {
                showToast(response.message, 'error');
            }
        },
        error: function() {
            showToast('Error setting acting role. Please try again.', 'error');
        }
    });
}

function submitExtendActing() {
    const formData = {
        userId: parseInt($('#extendActingUserId').val()),
        newActingEndDate: $('#extendActingNewEndDate').val(),
        currentActingEndDate: $('#extendActingCurrentEndDate').val()
    };

    $.ajax({
        url: window.userManagementUrls.extendActing,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function(response) {
            if (response.success) {
                $('#extendActingModal').modal('hide');
                showToast(response.message, 'success');
                refreshUserTable();
            } else {
                showToast(response.message, 'error');
            }
        },
        error: function() {
            showToast('Error extending acting period. Please try again.', 'error');
        }
    });
}

function submitStopActing(userId) {
    $.ajax({
        url: window.userManagementUrls.stopActing,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ userId: userId }),
        success: function(response) {
            if (response.success) {
                showToast(response.message, 'success');
                refreshUserTable();
            } else {
                showToast(response.message, 'error');
            }
        },
        error: function(xhr, status, error) {
            showToast('Error stopping acting role. Please try again.', 'error');
        }
    });
}

function validateSetActingForm(formData) {
    if (!formData.actingRoleId) {
        showToast('Please select an acting role', 'error');
        return false;
    }

    if (!formData.actingStartDate || !formData.actingEndDate) {
        showToast('Please select both start and end dates', 'error');
        return false;
    }

    const startDate = new Date(formData.actingStartDate);
    const endDate = new Date(formData.actingEndDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    if (startDate < today) {
        showToast('Start date cannot be in the past', 'error');
        return false;
    }

    if (endDate <= startDate) {
        showToast('End date must be after start date', 'error');
        return false;
    }

    return true;
}

function refreshUserTable() {
    // Trigger table refresh using AJAX filter functionality
    // Call the global applyFilters function from the page
    if (typeof applyFilters === 'function') {
        applyFilters();
    } else {
        // Fallback: reload page if applyFilters not available
        window.location.reload();
    }
}

// ============================================
// ACTING LOCATION FUNCTIONALITY
// ============================================

function initializeActingLocation() {
    // Load divisions when modal opens and show location fields immediately
    $('#setActingModal').on('shown.bs.modal', function() {
        // Always show acting location fields (no more optional)
        $('#actingLocationFields').show();

        // Load divisions on modal open
        loadActingDivisions();
    });

    // Handle division selection change
    $('#actingDivisionSelect').on('change', function() {
        const divisionId = $(this).val();
        if (divisionId) {
            loadActingDepartments(divisionId);
        } else {
            // Clear department dropdown when no division selected
            $('#actingDepartmentSelect').html('<option value="">-- Select Acting Department --</option>');
        }
    });

    // Reset location fields when modal is hidden
    $('#setActingModal').on('hidden.bs.modal', function() {
        resetActingLocationFields();
    });
}

function loadActingDivisions() {
    const $divisionSelect = $('#actingDivisionSelect');

    // Show loading state
    $divisionSelect.html('<option value="">Loading divisions...</option>');
    $divisionSelect.prop('disabled', true);

    $.ajax({
        url: '/UserManagement/GetActingDivisions',
        type: 'GET',
        success: function(response) {
            if (response.success) {
                populateActingDivisionDropdown(response.divisions);
            } else {
                showActingLocationError('Failed to load divisions: ' + response.message);
            }
        },
        error: function(xhr, status, error) {
            console.error('Error loading divisions:', error);
            showActingLocationError('Error loading divisions. Please try again.');
        },
        complete: function() {
            $divisionSelect.prop('disabled', false);
        }
    });
}

function populateActingDivisionDropdown(divisions) {
    const $divisionSelect = $('#actingDivisionSelect');

    // Clear and add default option
    $divisionSelect.html('<option value="">-- Select Acting Division --</option>');

    // Add divisions
    divisions.forEach(function(division) {
        if (division.value !== '') { // Skip the default "Select To Division" option
            $divisionSelect.append(`<option value="${division.value}">${division.text}</option>`);
        }
    });
}

function loadActingDepartments(divisionId) {
    const $departmentSelect = $('#actingDepartmentSelect');

    if (!divisionId) {
        $departmentSelect.html('<option value="">Select division first</option>');
        $departmentSelect.prop('disabled', true);
        return;
    }

    // Show loading state
    $departmentSelect.html('<option value="">Loading departments...</option>');
    $departmentSelect.prop('disabled', true);

    $.ajax({
        url: '/UserManagement/GetActingDepartmentsByDivision',
        type: 'GET',
        data: { divisionId: divisionId },
        success: function(response) {
            if (response.success) {
                populateActingDepartmentDropdown(response.departments);
            } else {
                showActingLocationError('Failed to load departments: ' + response.message);
            }
        },
        error: function(xhr, status, error) {
            console.error('Error loading departments:', error);
            showActingLocationError('Error loading departments. Please try again.');
        },
        complete: function() {
            $departmentSelect.prop('disabled', false);
        }
    });
}

function populateActingDepartmentDropdown(departments) {
    const $departmentSelect = $('#actingDepartmentSelect');

    // Clear and add default option
    $departmentSelect.html('<option value="">-- Select Acting Department --</option>');

    // Add departments
    departments.forEach(function(department) {
        $departmentSelect.append(`<option value="${department.id}">${department.name}</option>`);
    });
}

function clearActingLocationSelections() {
    $('#actingDivisionSelect').val('');
    $('#actingDepartmentSelect').html('<option value="">Select division first</option>');
    $('#actingDepartmentSelect').prop('disabled', true);
}

function resetActingLocationFields() {
    // Clear acting location selections (no more hiding fields - always visible)
    clearActingLocationSelections();
}

function showActingLocationError(message) {
    // You can customize this to show errors in a toast or alert
    console.error('Acting Location Error:', message);

    // Simple alert for now - can be replaced with toast notification
    alert('Location Error: ' + message);
}

function getActingLocationData() {
    // Acting location is now always required (no more optional)
    return {
        actingDivisionId: $('#actingDivisionSelect').val() || '',
        actingDepartmentId: $('#actingDepartmentSelect').val() || ''
    };
}