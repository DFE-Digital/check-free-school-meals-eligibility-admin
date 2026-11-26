document.addEventListener("DOMContentLoaded", function () {
    const ninoRadio = document.getElementById("identifierType-nino");
    const nassRadio = document.getElementById("identifierType-nass");
    const ninoInput = document.getElementById("NationalInsuranceNumber");
    const nassInput = document.getElementById("NationalAsylumSeekerServiceNumber");

    function updateIdentifierInputs() {
        ninoInput.disabled = !ninoRadio.checked;
        nassInput.disabled = !nassRadio.checked;
    }

    ninoRadio.addEventListener("change", updateIdentifierInputs);
    nassRadio.addEventListener("change", updateIdentifierInputs);
    updateIdentifierInputs();
});