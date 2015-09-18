function AddReview(storeLocation, productId) {
    var reviewTitle = $("#AddProductReview_Title").val();
    var reviewText = $("#AddProductReview_ReviewText").val();
    var reviewRating = $("input[id=AddProductReview_Rating]:radio:checked").val();
    // var reviewsCaptchaResponse = $("input[name=recaptcha_response_field]").val() || "";
    // var reviewsCaptchaChallenge = $("input[name=recaptcha_challenge_field]").val() || "";
    // + "&recaptcha_response_field=" + reviewsCaptchaResponse.toString() + "&recaptcha_challenge_field=" + reviewsCaptchaChallenge.toString()
    
    $("#updateTargetId")[0].style.opacity = 0.5;
    $.ajax({
        cache: false,
        type: "POST",
        url: storeLocation + "/ProductTab/ProductReviewsTabAddNew/" + productId,
        data: "add-review=Submit review" + "&AddProductReview.Title=" + reviewTitle.toString()
                     + "&AddProductReview.ReviewText=" + reviewText.toString() + "&AddProductReview.Rating=" + reviewRating.toString(),
        success: function (data) {
            $("#updateTargetId")[0].style.opacity = 1;
            $("#updateTargetId").replaceWith(data);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            $("#updateTargetId")[0].style.opacity = 1;
            alert('Failed to add review.');
        }
    });
}

function sendContactUsForm(storeLocation, productId) {
    var contactName = $(".contact_tab_fullname").val().toString();
    var contactEmail = $(".contact_tab_email").val().toString();
    var contactText = $(".contact_tab_enquiry").val().toString();
    
    $.ajax({
        cache: false,
        type: "POST",
        url: storeLocation + "ProductTab/ProductContactUsTabAddNew/" + productId,
        data: {
            FullName: contactName,
            Email: contactEmail,
            Enquiry: contactText
        },
        success: function (data) {
            $('#contact-us-tab').replaceWith(data);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            alert('Failed to contact us.');
        }
    });
}