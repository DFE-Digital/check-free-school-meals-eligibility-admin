function checkStatus() {
    let content = document.getElementById("content");
    let url = content.getAttribute("data-url");

    // Post back the check reference/parent details embedded in this page (rather than relying on
    // TempData/Session, which are shared across all tabs of the browser). See ELIG-3594.
    let responseJson = document.getElementById("loader-response")?.value ?? "";
    let parentGuardianJson = document.getElementById("loader-parent")?.value ?? "";
    let body = new URLSearchParams();
    body.append("responseJson", responseJson);
    body.append("parentGuardianJson", parentGuardianJson);

    fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: body.toString()
    })
        .then(response => {
            // Only follow this if the server itself redirected the poll somewhere else. Normalise
            // "url" (relative) to an absolute URL first - response.url is always absolute, so
            // comparing them directly would always mismatch and force a reload on every poll.
            var absoluteUrl = new URL(url, window.location.href).href;
            if (response.url && response.url !== absoluteUrl) {
                clearInterval(loaderTimer);
                window.location.href = response.url;
                return;
            }
            return response.text().then(html => {
                // Parse the fetched HTML and extract the #content section
                var parser = new DOMParser();
                var doc = parser.parseFromString(html, 'text/html');
                var newContent = doc.getElementById("content");

                // Only update the content if the data-type has changed
                if (newContent.getAttribute("data-type") !== document.getElementById("content").getAttribute("data-type")) {
                    document.getElementById("content").innerHTML = newContent.innerHTML;
                    document.getElementById("content").setAttribute("data-type", newContent.getAttribute("data-type"));
                    clearInterval(loaderTimer);

                    // Re-attach print handler
                    const printLink = document.getElementById("print-link");
                    if (printLink) {
                        printLink.addEventListener("click", (e) => { e.preventDefault(); window.print(); });
                    }
                }
            });
        })
        .catch(error => {
            console.error('Error fetching status:', error);
        });
}

// Poll the server for status if JavaScript is enabled
var loaderTimer = setInterval(function () {
    checkStatus();
}, 5000);