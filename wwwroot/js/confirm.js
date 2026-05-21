/**
 * ClientSphere — Confirmation Dialog Utility
 * Replaces native browser confirm() with SweetAlert2 modals.
 *
 * Usage (data attributes on any form submit button or link):
 *   data-confirm="true"                  — required to activate
 *   data-confirm-title="..."             — modal title (optional)
 *   data-confirm-text="..."              — modal body text (optional)
 *   data-confirm-icon="warning|error|question|info|success"  (optional, default: warning)
 *   data-confirm-ok="..."                — confirm button label (optional, default: "Yes, proceed")
 *   data-confirm-cancel="..."            — cancel button label (optional, default: "Cancel")
 *   data-confirm-ok-color="..."          — confirm button hex color (optional)
 *
 * Examples:
 *   <button type="submit" data-confirm="true"
 *           data-confirm-title="Deactivate User?"
 *           data-confirm-text="They will be locked out immediately."
 *           data-confirm-icon="warning"
 *           data-confirm-ok="Yes, deactivate"
 *           data-confirm-ok-color="#dc3545">
 *
 *   <button type="submit" data-confirm="true"
 *           data-confirm-title="Restore Campaign?"
 *           data-confirm-icon="question"
 *           data-confirm-ok="Yes, restore">
 */

(function () {
    'use strict';

    // Detect dark mode
    function isDarkMode() {
        return document.documentElement.getAttribute('data-bs-theme') === 'dark';
    }

    // Build SweetAlert2 options from data attributes
    function buildSwalOptions(el) {
        const title   = el.dataset.confirmTitle  || 'Are you sure?';
        const text    = el.dataset.confirmText   || 'This action cannot be undone.';
        const icon    = el.dataset.confirmIcon   || 'warning';
        const okText  = el.dataset.confirmOk     || 'Yes, proceed';
        const cancelText = el.dataset.confirmCancel || 'Cancel';
        const okColor = el.dataset.confirmOkColor || (icon === 'error' || icon === 'warning' ? '#dc3545' : '#0d6efd');

        return {
            title,
            text,
            icon,
            showCancelButton: true,
            confirmButtonText: okText,
            cancelButtonText: cancelText,
            confirmButtonColor: okColor,
            cancelButtonColor: '#6c757d',
            reverseButtons: true,
            focusCancel: true,
            background: isDarkMode() ? '#212529' : '#fff',
            color: isDarkMode() ? '#dee2e6' : '#212529',
        };
    }

    // Handle form submit buttons
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('button[data-confirm="true"]');
        if (!btn) return;

        const form = btn.closest('form');
        if (!form) return;

        e.preventDefault();
        e.stopImmediatePropagation();

        Swal.fire(buildSwalOptions(btn)).then(function (result) {
            if (result.isConfirmed) {
                // Disable button to prevent double-submit
                btn.disabled = true;
                form.submit();
            }
        });
    }, true);

    // Handle anchor links with data-confirm (for non-form destructive links)
    document.addEventListener('click', function (e) {
        const link = e.target.closest('a[data-confirm="true"]');
        if (!link) return;

        e.preventDefault();
        e.stopImmediatePropagation();

        const href = link.getAttribute('href');
        Swal.fire(buildSwalOptions(link)).then(function (result) {
            if (result.isConfirmed && href) {
                window.location.href = href;
            }
        });
    }, true);

})();
