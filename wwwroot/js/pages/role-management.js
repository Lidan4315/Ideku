// Role Management JavaScript
// Handles AJAX operations, form submissions, and UI interactions

$(document).ready(function() {
    initializeRoleManagement();
});

function initializeRoleManagement() {
    // Initialize form submissions
    initializeCreateRoleForm();
    initializeEditRoleForm();
    
    // Initialize button event handlers
    initializeEditButtons();
    initializeDeleteButtons();
    
    console.log('Role Management initialized');
}

// ============================================
// CREATE ROLE FUNCTIONALITY
// ============================================

function initializeCreateRoleForm() {
    $('#addRoleForm').on('submit', function(e) {
        e.preventDefault();
        
        const form = $(this);
        
        // Get form data for confirmation
        const roleName = $('#roleName').val().trim();
        const description = $('#description').val().trim();
        
        // Show confirmation dialog before saving
        Swal.fire({
            title: 'Save Role',
            html: `Are you sure you want to create the role "<strong>${roleName}</strong>"?<br><br>
                   <small class="text-muted">This will add a new role to the system.</small>`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#198754',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, Save Role',
            cancelButtonText: 'Cancel',
            reverseButtons: true
        }).then((result) => {
            if (result.isConfirmed) {
                saveNewRole(form, roleName, description);
            }
        });
    });
}

function saveNewRole(form, roleName, description) {
    // Show loading state
    Swal.fire({
        title: 'Creating Role...',
        text: 'Please wait while we create the role.',
        icon: 'info',
        allowOutsideClick: false,
        showConfirmButton: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
    
    // Get form data
    const formData = {
        roleName: roleName,
        description: description
    };
    
    // Add anti-forgery token
    const token = form.find('input[name="__RequestVerificationToken"]').val();
    
    // AJAX request to create role
    $.ajax({
        url: window.roleManagementUrls.create,
        type: 'POST',
        data: formData,
        headers: {
            'RequestVerificationToken': token
        },
        success: function(response) {
            if (response.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Created!',
                    text: response.message || 'Role created successfully.',
                    confirmButtonColor: '#198754',
                    timer: 2000,
                    showConfirmButton: false
                });
                
                // Close modal and reload page after short delay
                $('#addRoleModal').modal('hide');
                setTimeout(() => {
                    window.location.reload();
                }, 2000);
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Cannot Create Role',
                    text: response.message,
                    confirmButtonColor: '#dc2626'
                });
            }
        },
        error: function(xhr, status, error) {
            console.error('Create role error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'An error occurred while creating the role.',
                confirmButtonColor: '#dc2626'
            });
        }
    });
}

// ============================================
// EDIT ROLE FUNCTIONALITY
// ============================================

function initializeEditButtons() {
    $(document).on('click', '.edit-role-btn', function(e) {
        e.preventDefault();
        e.stopPropagation(); // Prevent row click
        
        const roleId = $(this).data('role-id');
        loadRoleForEdit(roleId);
    });
}

function loadRoleForEdit(roleId) {
    // Show loading in edit modal
    showEditModalLoading();
    
    $.ajax({
        url: window.roleManagementUrls.getRole,
        type: 'GET',
        data: { id: roleId },
        success: function(response) {
            if (response.success) {
                populateEditModal(response.role);
                $('#editRoleModal').modal('show');
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
            console.error('Load role error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Error!',
                text: 'Error loading role information.',
                confirmButtonColor: '#dc2626'
            });
        }
    });
}

function showEditModalLoading() {
    $('#editRoleId').val('');
    $('#editRoleName').val('Loading...');
    $('#editDescription').val('Loading...');
    $('#editRoleWarning').addClass('d-none');
}

function populateEditModal(role) {
    $('#editRoleId').val(role.id);
    $('#editRoleName').val(role.roleName);
    $('#editDescription').val(role.description || '');
    
    // Show warning if users are assigned
    if (role.userCount > 0) {
        $('#editRoleWarningText').text(`Warning: ${role.userCount} user(s) are assigned to this role.`);
        $('#editRoleWarning').removeClass('d-none');
    } else {
        $('#editRoleWarning').addClass('d-none');
    }
    
    // Update modal title
    $('#editRoleModalLabel').text(`Edit Role: "${role.roleName}"`);
}

function initializeEditRoleForm() {
    $('#editRoleForm').on('submit', function(e) {
        e.preventDefault();
        
        const form = $(this);
        
        // Get form data for confirmation
        const roleId = $('#editRoleId').val();
        const roleName = $('#editRoleName').val().trim();
        const description = $('#editDescription').val().trim();
        
        // Show confirmation dialog before updating
        Swal.fire({
            title: 'Update Role',
            html: `Are you sure you want to update the role "<strong>${roleName}</strong>"?<br><br>
                   <small class="text-muted">This will modify the role information in the system.</small>`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#198754',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, Update Role',
            cancelButtonText: 'Cancel',
            reverseButtons: true
        }).then((result) => {
            if (result.isConfirmed) {
                updateExistingRole(form, roleId, roleName, description);
            }
        });
    });
}

function updateExistingRole(form, roleId, roleName, description) {
    // Show loading state
    Swal.fire({
        title: 'Updating Role...',
        text: 'Please wait while we update the role.',
        icon: 'info',
        allowOutsideClick: false,
        showConfirmButton: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
    
    // Get form data
    const formData = {
        id: roleId,
        roleName: roleName,
        description: description
    };
    
    // Add anti-forgery token
    const token = form.find('input[name="__RequestVerificationToken"]').val();
    
    // AJAX request to update role
    $.ajax({
        url: window.roleManagementUrls.edit + '/' + roleId,
        type: 'POST',
        data: formData,
        headers: {
            'RequestVerificationToken': token
        },
        success: function(response) {
            if (response.success) {
                Swal.fire({
                    icon: 'success',
                    title: 'Updated!',
                    text: response.message || 'Role updated successfully.',
                    confirmButtonColor: '#198754',
                    timer: 2000,
                    showConfirmButton: false
                });
                
                // Close modal and reload page after short delay
                $('#editRoleModal').modal('hide');
                setTimeout(() => {
                    window.location.reload();
                }, 2000);
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Cannot Update Role',
                    text: response.message,
                    confirmButtonColor: '#dc2626'
                });
            }
        },
        error: function(xhr, status, error) {
            console.error('Update role error:', error);
            Swal.fire({
                icon: 'error',
                title: 'Error',
                text: 'An error occurred while updating the role.',
                confirmButtonColor: '#dc2626'
            });
        }
    });
}

// ============================================
// DELETE ROLE FUNCTIONALITY
// ============================================

function initializeDeleteButtons() {
    $(document).on('click', '.delete-role-btn', function(e) {
        e.preventDefault();
        e.stopPropagation(); // Prevent row click
        
        const roleId = $(this).data('role-id');
        const roleName = $(this).data('role-name');
        
        confirmDeleteRole(roleId, roleName);
    });
}

function confirmDeleteRole(roleId, roleName) {
    Swal.fire({
        title: 'Delete Role',
        html: `Are you sure you want to delete the role "<strong>${roleName}</strong>"?<br><br>
               <small class="text-muted">This action cannot be undone. Make sure no users are assigned to this role.</small>`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#dc3545',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, Delete',
        cancelButtonText: 'Cancel',
        reverseButtons: true
    }).then((result) => {
        if (result.isConfirmed) {
            deleteRole(roleId, roleName);
        }
    });
}

function deleteRole(roleId, roleName) {
    // Show loading state
    Swal.fire({
        title: 'Deleting Role...',
        text: 'Please wait while we delete the role.',
        icon: 'info',
        allowOutsideClick: false,
        showConfirmButton: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
    
    $.ajax({
        url: window.roleManagementUrls.delete,
        type: 'POST',
        data: { id: roleId },
        success: function(response) {
            if (response.success) {
                Swal.fire({
                    title: 'Deleted!',
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
                    title: 'Cannot Delete',
                    text: response.message,
                    icon: 'error',
                    confirmButtonText: 'OK'
                });
            }
        },
        error: function(xhr, status, error) {
            console.error('Delete role error:', error);
            Swal.fire({
                title: 'Error',
                text: 'An error occurred while deleting the role.',
                icon: 'error',
                confirmButtonText: 'OK'
            });
        }
    });
}

// ============================================
// REMOVED: Clickable rows functionality
// Role Management doesn't need Details page navigation
// ============================================

// ============================================
// UTILITY FUNCTIONS - Removed toast functions
// Now using direct Swal.fire() calls like ApproverManagement
// ============================================

// ============================================
// FORM VALIDATION ENHANCEMENTS
// ============================================

// Real-time validation for role name
$(document).on('input', '#roleName, #editRoleName', function() {
    const input = $(this);
    const value = input.val().trim();
    const form = input.closest('form');
    
    // Clear previous validation
    input.removeClass('is-invalid is-valid');
    input.siblings('.invalid-feedback').remove();
    
    if (value.length === 0) {
        input.addClass('is-invalid');
        input.after('<div class="invalid-feedback">Role name is required.</div>');
    } else if (value.length > 100) {
        input.addClass('is-invalid');
        input.after('<div class="invalid-feedback">Role name cannot exceed 100 characters.</div>');
    }
    // Removed is-valid styling - no green border/checkmark when valid
});

// Description validation
$(document).on('input', '#description, #editDescription', function() {
    const input = $(this);
    const value = input.val().trim();
    
    input.removeClass('is-invalid is-valid');
    input.siblings('.invalid-feedback').remove();
    
    if (value.length > 100) {
        input.addClass('is-invalid');
        input.after('<div class="invalid-feedback">Description cannot exceed 100 characters.</div>');
    }
    // Removed is-valid styling - no green border/checkmark when valid
});

// Clear validation when modals are closed
$('#addRoleModal, #editRoleModal').on('hidden.bs.modal', function() {
    const modal = $(this);
    modal.find('input, textarea').removeClass('is-invalid is-valid');
    modal.find('.invalid-feedback').remove();
    modal.find('form')[0].reset();
});