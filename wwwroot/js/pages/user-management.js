// User Management JavaScript
// Handles AJAX operations, form submissions, and UI interactions
// Following the same pattern as role-management.js for consistency

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
    
    console.log('User Management initialized');
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
    
    // Show warning if user has dependencies
    if (user.dependencyCount > 0) {
        $('#editUserWarningText').text(`Warning: This user has ${user.dependencyCount} associated record(s). Changes may affect system data.`);
        $('#editUserWarning').removeClass('d-none');
    } else {
        $('#editUserWarning').addClass('d-none');
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