let pendingChanges = [];


function loadFeatureDetails(featureId) {
    fetch(`/AdminM/FeatureDetails?featureId=${featureId}`)
        .then(response => response.text())
        .then(data => {
            // Display feature details in the preview pane
            document.getElementById('featurePreviewPane').innerHTML = data;
        })
        .catch(error => {
            console.error('There was a problem with the fetch operation:', error);
        });
}


function openFeatureDetailsModal(featureId) {
    fetch(`/AdminM/FeatureDetails?featureId=${featureId}`)
        .then(response => response.text())
        .then(data => {
            $('#featureDetailsModalContent').html(data);
            $('#featureDetailsModal').modal('show');
        })
        .catch(error => {
            console.error('There was a problem with the fetch operation:', error);
        });
}

// Remove the closeFeatureDetailsPopup function as it is not needed for Bootstrap modal.


function closeFeatureDetailsPopup() {

    $('#featureDetailsPopup').hide();
    $('#featureDetailsPopupContent').empty(); // Clear the content when hiding
}


function closeFeatureDetailsModal() {
    $('#featureDetailsModal').modal('hide');
}


function addChange(featureId, action, comment = '') {
    console.log('Pending Changes:', pendingChanges);

    const existingChangeIndex = pendingChanges.findIndex(
        (change) => change.featureId === featureId
    );

    if (existingChangeIndex !== -1) {
        // Check if the action has changed
        if (pendingChanges[existingChangeIndex].action !== action) {
            // Update the existing change
            pendingChanges[existingChangeIndex].action = action;
            pendingChanges[existingChangeIndex].comment = comment;
        }
    } else {
        // Add a new change
        pendingChanges.push({ featureId, action, comment });
    }

    // Update the pending changes preview in the modal without calling another function
    var previewContent = '<h5>Changes Preview:</h5><ul>';
    pendingChanges.forEach((change) => {
        previewContent += `<li>${change.action} for feature ID ${change.featureId}</li>`;
    });
    previewContent += '</ul>';

    var previewBody = document.getElementById('previewChangesBody');
    if (previewBody) {
        previewBody.innerHTML = previewContent;
    }
}






function saveComment(featureId) {
    var commentTextArea = document.getElementById(`commentTextArea_${featureId}`);
    var comment = commentTextArea.value.trim();
    console.log('Feature ID:', featureId);

    // Show SweetAlert2 confirmation dialog
    Swal.fire({
        title: "Are you sure?",
        text: "You want to add comment?",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, add comment!"
    }).then((result) => {
        if (result.isConfirmed) {
            // User clicked "Yes"

            console.log(featureId);
            fetch(`/AdminM/UpdateComment?featureId=${featureId}&comment=${encodeURIComponent(comment)}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            })
                .then(response => {
                    if (response.ok) {
                        // Comment accepted successfully
                        addChange(featureId, 'Comment', comment);
                        $('#commentModal').modal('hide');
                        Swal.fire({
                            title: "Comment Added!",
                            text: "Your comment has been added.",
                            icon: "success",
                            timer: 2000,
                            showConfirmButton: false
                        });
                    } else {
                        console.error('Failed to comment feature');
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                });
        }
    });
}

function acceptFeature(featureId) {
    var acceptButton = document.querySelector(`button[data-feature-id="${featureId}"][onclick^="acceptFeature"]`);
    var rejectButton = document.querySelector(`button[data-feature-id="${featureId}"][onclick^="rejectFeature"]`);

    Swal.fire({
        title: "Are you sure?",
        text: "You want to accept the feature?",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#28a745", // green color for the confirm button
        cancelButtonColor: "#3085d6",
        confirmButtonText: "Yes, accept it!"
    }).then((result) => {
        if (result.isConfirmed) {
            if (acceptButton && acceptButton.innerText !== 'Approved') {
                acceptButton.innerText = 'Approved';
                acceptButton.classList.add('btn-outline-success');
                acceptButton.classList.remove('btn-outline-primary');
            }

            if (rejectButton && rejectButton.innerText !== 'Reject') {
                rejectButton.innerText = 'Reject';
                rejectButton.classList.remove('d-none');
                rejectButton.classList.remove('btn-outline-danger');
                rejectButton.classList.add('btn-outline-primary');
            }

            fetch(`/AdminM/AcceptFeature?featureId=${featureId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            })
                .then(response => {
                    if (response.ok) {
                        console.log('Feature accepted successfully');
                        addChange(featureId, 'Accept');
                    } else {
                        console.error('Failed to accept feature');
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                });
        }
    });
}

function rejectFeature(featureId) {
    var acceptButton = document.querySelector(`button[data-feature-id="${featureId}"][onclick^="acceptFeature"]`);
    var rejectButton = document.querySelector(`button[data-feature-id="${featureId}"][onclick^="rejectFeature"]`);

    Swal.fire({
        title: "Are you sure?",
        text: "You want to reject the feature?",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#d33",
        cancelButtonColor: "#3085d6",
        confirmButtonText: "Yes, reject it!"
    }).then((result) => {
        if (result.isConfirmed) {
            if (rejectButton && rejectButton.innerText !== 'Rejected') {
                rejectButton.innerText = 'Rejected';
                rejectButton.classList.add('btn-outline-danger');
                rejectButton.classList.remove('btn-outline-primary');
            }

            if (acceptButton && acceptButton.innerText !== 'Accept') {
                acceptButton.innerText = 'Accept';
                acceptButton.classList.remove('btn-outline-success');
                acceptButton.classList.add('btn-outline-primary');
            }

            fetch(`/AdminM/RejectFeature?featureId=${featureId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            })
                .then(response => {
                    if (response.ok) {
                        console.log('Feature rejected successfully');
                        addChange(featureId, 'Reject');
                    } else {
                        console.error('Failed to reject feature');
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                });
        }
    });
}

function updateAllChanges() {
    // Create a preview of changes
    var previewContent = '<h5>Changes Preview:</h5><ul>';
    pendingChanges.forEach(change => {
        previewContent += `<li>${change.action} for feature ID ${change.featureId}</li>`;
    });
    previewContent += '</ul>';

    // Set the preview content inside the modal body
    var previewBody = document.getElementById('previewChangesBody');
    if (previewBody) {
        previewBody.innerHTML = previewContent;
    }

    // Show the modal
    $('#previewModal').modal('show');

    // Handle the OK button click to proceed with updates
    $('#confirmUpdateButton').on('click', function () {
        // Assuming you want to send changes to the server using fetch
        fetch('/AdminM/UpdateAllChanges', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(pendingChanges)
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                // Handle success, e.g., clear changes or update UI to reflect changes
                pendingChanges = []; // Clear changes after successful update
                $('#previewModal').modal('hide'); // Close the modal
                Swal.fire({
                    title: 'Success!',
                    text: 'All changes have been successfully updated.',
                    icon: 'success',
                    timer: 2000, // Show for 2 seconds
                    showConfirmButton: false
                }).then(() => {
                    // After the timer finishes, reload the page
                    location.reload();
                });

               

                
            })
            .catch(error => {
                // Handle errors
                console.error('There was a problem with the fetch operation:', error);
            });
    });

    // Handle the Cancel button click to close the modal
    $('#cancelUpdateButton').on('click', function () {
        $('#previewModal').modal('hide'); // Close the modal
    });

    // Handle the "Clear All" button click to clear all changes and the preview content
    $('#clearAllChangesButton').on('click', function () {
        fetch('/AdminM/ClearPendingChanges', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            }
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }
                // Optional: Handle UI update or other tasks after clearing
                console.log('Pending changes cleared successfully');

            })
            .catch(error => {
                // Handle errors
                console.error('There was a problem with the fetch operation:', error);
            });

        pendingChanges = []; // Clear pending changes
        $('#previewChangesBody').empty(); // Clear the preview content
        $('#previewModal').modal('hide'); // Close the modal
        location.reload(); // Refresh the page

    });
}

