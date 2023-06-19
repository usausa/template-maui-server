"use strict";
var App;
(function (app) {
    app.clickUrl = (url) => {
        const link = document.createElement("a");
        document.body.appendChild(link);
        link.href = url;
        link.target = "_blank";
        link.click();
        document.body.removeChild(link);
    }
})(App || (App = {}));
