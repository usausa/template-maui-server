"use strict";
var Progress;
(function (progress) {
    progress.showProgress = (id) => {
        document.getElementById(id).showModal();
    }

    progress.hideProgress = (id) => {
        document.getElementById(id).close();
    }
})(Progress || (Progress = {}));
