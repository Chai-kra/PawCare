$(function () {
    const $form = $('#bookingForm');
    if ($form.length === 0) return;

    const slotsUrl = $form.data('slots-url');
    let pendingSlot = $form.data('initial-slot') || '';

    const $staff = $('#StaffId');
    const $petName = $('#PetName');
    const $date = $('#Date');
    const $slotBox = $('#slotBox');
    const $slotTime = $('#SlotTime');

    function selectedPackage() {
        return $('.package-radio:checked').val() || '';
    }

    function updateSummary() {
        const $p = $('.package-radio:checked');
        const minutes = $p.length ? Number($p.data('duration')) : 0;
        const price = $p.length ? Number($p.data('price')) : 0;
        $('#sumDuration').text(minutes + ' min');
        $('#sumPrice').text('RM ' + price.toFixed(2));
    }

    function loadSlots() {
        $slotTime.val('');
        $('#slotError').text('')

        if (!selectedPackage() || !$date.val()) {
            $slotBox.html('<p class="hint>Select a service package and date to see availability')
            return;
        }

        $slotBox.html('<p class="hint">Loading available times...</p>');

        $.ajax({
            url: slotsUrl,
            type: 'GET',
            data: {
                date: $date.val(),
                servicePackageId: selectedPackage(),
                staffId: $staff.val(),
                petName: $petName.val()
            }
        })
            .done(function (html) {
                $slotBox.html(html);
                if (pendingSlot) {
                    $slotBox.find('.slot-radio[value="' + pendingSlot + '"]').prop('checked', true);
                    $slotTime.val(pendingSlot);
                    pendingSlot = '';
                }
            })
            .fail(function () {
                $slotBox.html('<p class="error-message">Could not load time slots. Please try again.</p>');
            });
    }

    $slotBox.on('change', '.slot-radio', function () {
        $slotTime.val($(this).val());
        $('#slotError').text('');
    });

    $('.package-radio').on('change', function () { $('#packageError').text(''); updateSummary(); loadSlots(); });
    $staff.add($petName).add($date).on('change', loadSlots);

    $form.on('submit', function (e) {
        let ok = true;
        if (!selectedPackage()) {
            $('#packageError').text('Please select a service package.');
            ok = false;
        }
        if (!$slotTime.val()) {
            $('#slotError').text('Please select a time slot.');
            ok = false;
        }
        if (!ok) e.preventDefault();
    });

    updateSummary();
    loadSlots();
});