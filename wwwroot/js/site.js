document.addEventListener("DOMContentLoaded", function () {
    const popup = document.getElementById("guestSupportPopup");
    const closeButton = document.getElementById("guestSupportPopupClose");
    const storageKey = "carpoint_guest_support_popup_closed";

    if (!popup) {
        return;
    }

    if (localStorage.getItem(storageKey) === "true") {
        popup.classList.add("d-none");
    }

    closeButton?.addEventListener("click", function () {
        popup.classList.add("d-none");
        localStorage.setItem(storageKey, "true");
    });
});
