// Galleries Management JavaScript

// Show Create Gallery Modal
function showCreateGalleryModal() {
    $.ajax({
        url: '/Galleries/Create',
        type: 'GET',
        success: function (result) {
            $('#createGalleryModal').html(result);
            const modal = new bootstrap.Modal(document.getElementById('createGalleryModalElement'));
            modal.show();
        },
        error: function (xhr, status, error) {
            showToast('Error', 'Failed to load create gallery form.', 'error');
            console.error('Error:', error);
        }
    });
}

// Show Edit Gallery Modal
function showEditGalleryModal(id) {
    $.ajax({
        url: `/Galleries/Edit/${id}`,
        type: 'GET',
        success: function (result) {
            $('#editGalleryModal').html(result);
            const modal = new bootstrap.Modal(document.getElementById('editGalleryModalElement'));
            modal.show();
        },
        error: function (xhr, status, error) {
            showToast('Error', 'Failed to load edit gallery form.', 'error');
            console.error('Error:', error);
        }
    });
}

// Show Gallery Details Modal
function showGalleryDetails(id) {
    $.ajax({
        url: `/Galleries/Details/${id}`,
        type: 'GET',
        success: function (result) {
            $('#galleryDetailsModal').html(result);
            const modal = new bootstrap.Modal(document.getElementById('galleryDetailsModalElement'));
            modal.show();
        },
        error: function (xhr, status, error) {
            showToast('Error', 'Failed to load gallery details.', 'error');
            console.error('Error:', error);
        }
    });
}

// Show Gallery Sessions Modal
function showSessions(id) {
    $.ajax({
        url: `/Galleries/Sessions/${id}`,
        type: 'GET',
        success: function (result) {
            $('#gallerySessionsModal').html(result);
            const modal = new bootstrap.Modal(document.getElementById('gallerySessionsModalElement'));
            modal.show();
        },
        error: function (xhr, status, error) {
            showToast('Error', 'Failed to load gallery sessions.', 'error');
            console.error('Error:', error);
        }
    });
}

// Show Manage Access Modal
function showManageAccessModal(id) {
    $.ajax({
        url: `/Galleries/ManageAccess/${id}`,
        type: 'GET',
        success: function (result) {
            $('#manageAccessModal').html(result);
            const modal = new bootstrap.Modal(document.getElementById('manageAccessModalElement'));
            modal.show();
        },
        error: function (xhr, status, error) {
            showToast('Error', 'Failed to load access management form.', 'error');
            console.error('Error:', error);
        }
    });
}

// Handle Create Gallery Form Submission
$(document).on('submit', '#createGalleryForm', function (e) {
    e.preventDefault();

    const form = $(this);
    const btn = $('#createGalleryBtn');
    const btnText = btn.find('.btn-text');
    const btnLoading = btn.find('.btn-loading');

    btnText.addClass('d-none');
    btnLoading.removeClass('d-none');
    btn.prop('disabled', true);

    $.ajax({
        url: form.attr('action'),
        type: 'POST',
        data: form.serialize(),
        success: function (response) {
            if (response.success) {
                showToast('Success', response.message, 'success');

                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('createGalleryModalElement'));
                if (modal) {
                    modal.hide();
                }

                // Reload page to show new gallery
                setTimeout(() => {
                    location.reload();
                }, 1500);
            } else {
                showToast('Error', response.message, 'error');
                btnText.removeClass('d-none');
                btnLoading.addClass('d-none');
                btn.prop('disabled', false);
            }
        },
        error: function (xhr, status, error) {
            showToast('Error', 'An error occurred while creating the gallery.', 'error');
            console.error('Error:', error);
            btnText.removeClass('d-none');
            btnLoading.addClass('d-none');
            btn.prop('disabled', false);
        }
    });
});

// Handle Edit Gallery Form Submission
$(document).on('submit', '#editGalleryForm', function (e) {
    e.preventDefault();

    const form = $(this);
    const btn = $('#editGalleryBtn');
    const btnText = btn.find('.btn-text');
    const btnLoading = btn.find('.btn-loading');

    btnText.addClass('d-none');
    btnLoading.removeClass('d-none');
    btn.prop('disabled', true);

    $.ajax({
        url: form.attr('action'),
        type: 'POST',
        data: form.serialize(),
        success: function (response) {
            if (response.success) {
                showToast('Success', response.message, 'success');

                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('editGalleryModalElement'));
                if (modal) {
                    modal.hide();
                }

                // Reload page to show updated gallery
                setTimeout(() => {
                    location.reload();
                }, 1500);
            } else {
                showToast('Error', response.message, 'error');
                btnText.removeClass('d-none');
                btnLoading.addClass('d-none');
                btn.prop('disabled', false);
            }
        },
        error: function (xhr, status, error) {
            showToast('Error', 'An error occurred while updating the gallery.', 'error');
            console.error('Error:', error);
            btnText.removeClass('d-none');
            btnLoading.addClass('d-none');
            btn.prop('disabled', false);
        }
    });
});

// Delete Gallery
function deleteGallery(id, name) {
    if (!confirm(`Are you sure you want to delete the gallery "${name}"? This action cannot be undone.`)) {
        return;
    }

    const token = $('input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: `/Galleries/Delete/${id}`,
        type: 'POST',
        headers: {
            'RequestVerificationToken': token
        },
        data: {
            __RequestVerificationToken: token
        },
        success: function (response) {
            if (response.success) {
                showToast('Success', response.message, 'success');
                setTimeout(() => {
                    location.reload();
                }, 1500);
            } else {
                showToast('Error', response.message, 'error');
            }
        },
        error: function (xhr, status, error) {
            showToast('Error', 'An error occurred while deleting the gallery.', 'error');
            console.error('Error:', error);
        }
    });
}

// Toggle Gallery Status
function toggleStatus(id, isActive) {
    const action = isActive ? 'activate' : 'deactivate';
    const token = $('input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: '/Galleries/ToggleStatus',
        type: 'POST',
        headers: {
            'RequestVerificationToken': token
        },
        data: {
            id: id,
            isActive: isActive,
            __RequestVerificationToken: token
        },
        success: function (response) {
            if (response.success) {
                showToast('Success', response.message, 'success');
                setTimeout(() => {
                    location.reload();
                }, 1000);
            } else {
                showToast('Error', response.message, 'error');
            }
        },
        error: function (xhr, status, error) {
            showToast('Error', `An error occurred while ${action}ing the gallery.`, 'error');
            console.error('Error:', error);
        }
    });
}

// Copy Access URL
function copyAccessUrl(id) {
    $.ajax({
        url: `/Galleries/GetAccessUrl/${id}`,
        type: 'GET',
        success: function (response) {
            if (response.success) {
                copyToClipboard(response.url, 'Gallery URL');
            } else {
                showToast('Error', response.message, 'error');
            }
        },
        error: function (xhr, status, error) {
            showToast('Error', 'Failed to get gallery URL.', 'error');
            console.error('Error:', error);
        }
    });
}

// End Session
function endSession(sessionId) {
    if (!confirm('Are you sure you want to end this session?')) {
        return;
    }

    const token = $('input[name="__RequestVerificationToken"]').val();

    $.ajax({
        url: '/Galleries/EndSession',
        type: 'POST',
        headers: {
            'RequestVerificationToken': token
        },
        data: {
            sessionId: sessionId,
            __RequestVerificationToken: token
        },
        success: function (response) {
            if (response.success) {
                showToast('Success', response.message, 'success');
                setTimeout(() => {
                    location.reload();
                }, 1000);
            } else {
                showToast('Error', response.message, 'error');
            }
        },
        error: function (xhr, status, error) {
            showToast('Error', 'An error occurred while ending the session.', 'error');
            console.error('Error:', error);
        }
    });
}

// Copy to Clipboard Utility
function copyToClipboard(text, label) {
    if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text).then(function () {
            showToast('Success', `${label} copied to clipboard!`, 'success');
        }).catch(function (err) {
            console.error('Failed to copy:', err);
            fallbackCopyToClipboard(text, label);
        });
    } else {
        fallbackCopyToClipboard(text, label);
    }
}

function fallbackCopyToClipboard(text, label) {
    const textArea = document.createElement('textarea');
    textArea.value = text;
    textArea.style.position = 'fixed';
    textArea.style.left = '-999999px';
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();

    try {
        document.execCommand('copy');
        showToast('Success', `${label} copied to clipboard!`, 'success');
    } catch (err) {
        showToast('Error', 'Failed to copy to clipboard.', 'error');
        console.error('Fallback copy failed:', err);
    }

    document.body.removeChild(textArea);
}

// Toast Notification Utility
function showToast(title, message, type) {
    if (typeof toastr !== 'undefined') {
        toastr.options = {
            "closeButton": true,
            "progressBar": true,
            "positionClass": "toast-top-right",
            "timeOut": "3000"
        };
        toastr[type](message, title);
    } else {
        alert(`${title}: ${message}`);
    }
}
