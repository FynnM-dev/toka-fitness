// Hide password checkbox function
function unhidePass() {
    var passInput = document.getElementById("passInput");
    if (passInput.type === "password") {
        passInput.type = "text";
    }
    else {
            passInput.type = "password";
    }
}