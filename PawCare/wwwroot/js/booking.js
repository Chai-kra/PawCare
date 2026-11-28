$(function () {
    const $form = $('#bookingForm');
    if (!$form.length) return;

    const slotsUrl = $form.data('slots-url');
    let initialSlot = $form.data('initial-slot') || '';

    function selectedPackage() {
        return $('.package-radio:checked').val() || '';
    }

    function updateSummary() {
        const $pkg = $('.package-radio:checked');
        const minutes = $pkg.length ? Number($pkg.data('duration')) || 0 : 0;
        const price = $pkg.length ? Number($pkg.data('price')) || 0 : 0;

        $('#sumDuration').text(minutes + ' min');
        $('#sumPrice').text('RM ' + price.toFixed(2));
    }

    function loadSlots() {
        const date = $('#Date').val();
        const servicePackageId = selectedPackage();

        $('#SlotTime').val('');

        if (!date || !servicePackageId) {
            $('#slotBox').html('<p class="hint">Select a date and a service to see available times.</p>');
            return;
        }

        $('#slotBox').html('<p class="hint">Loading available times...</p>');

        $.get(slotsUrl, {
            date: date,
            servicePackageId: servicePackageId,
            staffId: $('#StaffId').val(),
            petName: $('#PetName').val()
        })
            .done(function (html) {
                $('#slotBox').html(html);

                if (initialSlot) {
                    const $radio = $('#slotBox .slot-radio[value="' + initialSlot + '"]');
                    if ($radio.length) {
                        $radio.prop('checked', true);
                        $('#SlotTime').val(initialSlot);
                    }
                    initialSlot = '';
                }
            })
            .fail(function () {
                $('#slotBox').html('<p class="error-message">Unable to load time slots.</p>');
            });
    }

    $('#slotBox').on('change', '.slot-radio', function () {
        $('#SlotTime').val($(this).val());
    });

    $(document).on('change', '.package-radio', function () {
        updateSummary();
        loadSlots();
    });

    $('#Date, #StaffId, #PetName').on('change', loadSlots);

    updateSummary();
    loadSlots();
});