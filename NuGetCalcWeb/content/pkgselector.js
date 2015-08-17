/// <reference path="../Scripts/jquery-2.1.4.js" />
/// <reference path="../Scripts/jquery-2.1.4.intellisense.js" />
/// <reference path="../Scripts/typeahead.bundle.js" />

(function (undefined) {
    $("#form-compatilibity").on("submit", function () {
        var targetFramework = $("#compatibility-target-framework").val();
        if (!targetFramework) {
            alert("Target Framework is required.");
            return false;
        }

        if ($("#form-browse").hasClass("active")) {
            var packageId = $("#package-id").val();
            if (!packageId) {
                alert("Package ID is required.");
                return false;
            }

            $("#compatibility-source").val($("#package-source").val());
            $("#compatibility-id").val(packageId);
            $("#compatibility-version").val($("#package-version").val());
            return true;
        } else {
            $("#upload-method").val("compatibility");
            $("#upload-target-framework").val(targetFramework);
            $("#form-upload-browse").submit();
            return false;
        }
    });

    // Supports only official repository
    $("#package-id").typeahead({ minLength: 0 }, {
        source: function (query, syncCallback, asyncCallback) {
            $.getJSON("https://api-v3search-0.nuget.org/autocomplete?callback=?", { q: query })
                .done(function (data) {
                    asyncCallback(data.data);
                });
        },
        limit: 10
    });

    var versionCache = {};
    var queryFilter = function (versions, query) {
        return query ? versions.filter(function(x) { return x.indexOf(query) === 0; }) : versions;
    };
    $("#package-version").typeahead({ minLength: 0 }, {
        source: function (query, syncCallback, asyncCallback) {
            var packageId = $("#package-id").typeahead("val").toLowerCase();
            if (!packageId) return;
            var versions = versionCache[packageId];
            if (versions != undefined) {
                if (versions)
                    syncCallback(queryFilter(versions, query));
            } else {
                $.getJSON("https://api-v3search-0.nuget.org/autocomplete?callback=?", { id: packageId })
                    .done(function (res) {
                        var data = res.data.reverse();
                        versionCache[packageId] = data;
                        asyncCallback(queryFilter(data, query));
                    })
                    .fail(function () {
                        versionCache[packageId] = null;
                    });
            }
        },
        limit: 10
    });
})();
