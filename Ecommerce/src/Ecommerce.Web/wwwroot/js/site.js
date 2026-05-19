function addToCart(productId, quantity) {
    quantity = quantity || 1;
    $.post('/Cart/Add', { productId: productId, quantity: quantity }, function (res) {
        if (res.success) {
            updateBadge(res.totalItems);
            showToast('Item added to cart!');
        }
    });
}

function updateBadge(count) {
    var badge = $('#cart-badge');
    if (count > 0) {
        badge.text(count).show();
    } else {
        badge.hide();
    }
}

function showToast(message) {
    var toast = $('<div class="toast-notification">' + message + '</div>').appendTo('body');
    setTimeout(function () { toast.addClass('show'); }, 10);
    setTimeout(function () { toast.removeClass('show'); setTimeout(function () { toast.remove(); }, 300); }, 2000);
}

$(function () {
    $.get('/Cart/Count', function (res) { updateBadge(res.totalItems); });

    $(document).on('click', '.btn-add-to-cart', function () {
        var productId = $(this).data('product-id');
        addToCart(productId, 1);
    });
});
