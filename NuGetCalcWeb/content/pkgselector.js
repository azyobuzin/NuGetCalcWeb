/// <reference path="../Scripts/jquery-2.1.3.js" />

$("#form-compatilibity").on("submit", function () {
    var packageId = $("#package-id").val();
    if (packageId == "") {
        alert("Package ID is required.");
        return false;
    }
    if ($("#compatibility-target-framework").val() == "") {
        alert("Target Framework is required.");
        return false;
    }

    $("#compatibility-source").val($("#package-source").val());
    $("#compatibility-id").val(packageId);
    $("#compatibility-version").val($("#package-version").val());
    return true;
});
