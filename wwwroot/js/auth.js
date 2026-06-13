(function () {
    if (window.__authInitialized) return;
    window.__authInitialized = true;

    var refreshPromise = null;
    var authInitPromise = null;
    var authReady = false;
    var authSessionState = null;

    function isAuthUrl(url) {
        var value = typeof url === "string" ? url : (url && url.url) || "";
        return value.toLowerCase().indexOf("/api/auth/refresh") >= 0 ||
            value.toLowerCase().indexOf("/api/auth/login") >= 0 ||
            value.toLowerCase().indexOf("/api/auth/logout") >= 0;
    }

    function mergeHeaders(existing, extra) {
        var headers = new Headers(existing || {});
        Object.keys(extra || {}).forEach(function (key) {
            if (extra[key] !== undefined && extra[key] !== null) {
                headers.set(key, extra[key]);
            }
        });
        return headers;
    }

    async function refreshAuthCookie() {
        if (refreshPromise) return refreshPromise;

        refreshPromise = window.__nativeFetch("/websession/checksession", {
            method: "GET",
            credentials: "include",
            headers: { "Content-Type": "application/json" }
        })
            .then(async function (response) {
                if (!response.ok) {
                    throw new Error("Unable to verify authentication");
                }

                var data = await response.json();
                if (!data || data.valid !== true) {
                    throw new Error("Invalid session");
                }

                authSessionState = {
                    userId: Number(data.userId) || 0,
                    userName: data.userName || "",
                    selectedCompanyId: Number(data.selectedCompanyId) || 0,
                    companies: Array.isArray(data.companies) ? data.companies : []
                };

                authReady = true;
                return true;
            })
            .finally(function () {
                refreshPromise = null;
            });

        return refreshPromise;
    }

    function redirectToLogin() {
        if (!window.location.pathname.toLowerCase().includes("/login")) {
            window.location.href = "/Login";
        }
    }

    function initializeAuth(options) {
        options = options || {};
        if (authReady) return Promise.resolve(true);
        if (authInitPromise) return authInitPromise;

        authInitPromise = refreshAuthCookie()
            .catch(function (error) {
                authReady = false;
                if (options.redirectOnFailure !== false) {
                    redirectToLogin();
                }
                throw error;
            })
            .finally(function () {
                authInitPromise = null;
            });

        return authInitPromise;
    }

    async function authFetch(input, init, retrying) {
        var options = Object.assign({}, init || {});
        options.credentials = options.credentials || "include";

        var response = await window.__nativeFetch(input, options);
        if ((response.status === 401 || response.status === 403) && !isAuthUrl(input)) {
            authReady = false;
            redirectToLogin();
            return response;
        }

        return response;
    }

    window.__nativeFetch = window.__nativeFetch || window.fetch.bind(window);
    window.fetch = function (input, init) {
        return authFetch(input, init, false);
    };

    window.APP = window.APP || {};

    window.APP.setAccessToken = function (token) {
        authReady = !!token || authReady;
    };

    window.APP.clearAccessToken = function () {
        authReady = false;
    };

    window.APP.ensureAccessToken = refreshAuthCookie;
    window.APP.initializeAuth = initializeAuth;
    window.APP.getSessionState = function () {
        return authSessionState;
    };
    window.APP.isAuthReady = function () {
        return authReady;
    };
    window.APP.redirectToLogin = redirectToLogin;

    window.APP.getAuthHeaders = function () {
        return { "Content-Type": "application/json" };
    };

    window.APP.getAuthFetch = function (url, options) {
        options = options || {};
        options.headers = mergeHeaders(options.headers, window.APP.getAuthHeaders());
        options.credentials = options.credentials || "include";
        return fetch(url, options);
    };

    function setupJQueryAuth() {
        if (typeof jQuery === "undefined") return;
        var $ = jQuery;

        $.ajaxSetup({
            xhrFields: { withCredentials: true }
        });

        if (!$.ajax.__authWrapped) {
            var nativeAjax = $.ajax.bind($);

            $.ajax = function (url, options) {
                var settings = typeof url === "object"
                    ? Object.assign({}, url)
                    : Object.assign({}, options || {}, { url: url });

                if (isAuthUrl(settings.url)) {
                    return nativeAjax(settings);
                }

                var success = settings.success;
                var error = settings.error;
                var complete = settings.complete;
                var context = settings.context || settings;
                var deferred = $.Deferred();
                var currentRequest = null;

                delete settings.success;
                delete settings.error;
                delete settings.complete;

                function invoke(callback, args) {
                    if (!callback) return;
                    if ($.isArray && $.isArray(callback)) {
                        callback.forEach(function (fn) {
                            if (typeof fn === "function") fn.apply(context, args);
                        });
                    } else if (typeof callback === "function") {
                        callback.apply(context, args);
                    }
                }

                function run(retrying) {
                    var willRetry = false;
                    currentRequest = nativeAjax(settings)
                        .done(function (data, textStatus, jqXHR) {
                            invoke(success, [data, textStatus, jqXHR]);
                            deferred.resolveWith(context, [data, textStatus, jqXHR]);
                        })
                        .fail(function (jqXHR, textStatus, errorThrown) {
                            if (jqXHR && jqXHR.status === 401 && !retrying) {
                                authReady = false;
                                redirectToLogin();
                                deferred.rejectWith(context, [jqXHR, textStatus, errorThrown]);
                                return;
                            }

                            invoke(error, [jqXHR, textStatus, errorThrown]);
                            deferred.rejectWith(context, [jqXHR, textStatus, errorThrown]);
                        })
                        .always(function () {
                            if (!willRetry) {
                                invoke(complete, arguments);
                            }
                        });
                }

                initializeAuth({ redirectOnFailure: false })
                    .then(function () { run(false); })
                    .catch(function () { run(false); });

                var promise = deferred.promise();
                promise.abort = function () {
                    if (currentRequest && currentRequest.abort) {
                        currentRequest.abort();
                    }
                    deferred.rejectWith(context, [currentRequest, "abort", "abort"]);
                    return promise;
                };
                return promise;
            };

            $.ajax.__authWrapped = true;
        }

        $(document).ajaxError(function (event, xhr, settings, thrownError) {
            if (xhr.status === 0 || thrownError === "abort") return;
            if (xhr.status === 401) {
                console.warn("API returned 401 (unauthorized).");
                authReady = false;
                redirectToLogin();
                return;
            }
            if (xhr.status === 403) {
                authReady = false;
                redirectToLogin();
                return;
            }
            if (xhr.status === 429) {
                if (window.APP && window.APP.showError) {
                    window.APP.showError("Too Many Requests", "Please wait a moment and try again.");
                }
                return;
            }
            var msg = "An unexpected error occurred";
            try {
                var resp = xhr.responseJSON;
                if (resp && resp.message) msg = resp.message;
                else if (resp && resp.Message) msg = resp.Message;
            } catch (e) { }
            if (thrownError && thrownError !== "error") msg = thrownError;
            console.error("AJAX Error:", xhr.status, msg);
            if (window.APP && window.APP.showApiError) {
                window.APP.showApiError(msg);
            }
        });
    }

    window.APP.showError = function (title, message) {
        if (title === "Access Denied") {
            redirectToLogin();
            return;
        }

        var overlay = document.getElementById("appErrorOverlay");
        if (!overlay) {
            overlay = document.createElement("div");
            overlay.id = "appErrorOverlay";
            overlay.style.cssText = "position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.5);z-index:99999;display:flex;align-items:center;justify-content:center;";
            overlay.innerHTML = '<div id="appErrorBox" style="background:#fff;border-radius:12px;padding:40px 50px;max-width:450px;width:90%;text-align:center;box-shadow:0 20px 60px rgba(0,0,0,0.3);"><div id="appErrorIcon" style="font-size:48px;margin-bottom:15px;"></div><h3 id="appErrorTitle" style="margin:0 0 10px;color:#1e293b;font-size:20px;"></h3><p id="appErrorMsg" style="margin:0 0 25px;color:#64748b;font-size:14px;line-height:1.5;"></p><button onclick="window.APP.hideError()" style="background:#0b2c4d;color:#fff;border:none;padding:10px 30px;border-radius:8px;font-size:14px;cursor:pointer;font-weight:600;">OK</button></div>';
            document.body.appendChild(overlay);
        }
        document.getElementById("appErrorIcon").textContent = title === "Access Denied" ? "\uD83D\uDD12" : "\u26A0\uFE0F";
        document.getElementById("appErrorTitle").textContent = title;
        document.getElementById("appErrorMsg").textContent = message;
        overlay.style.display = "flex";
    };

    window.APP.showApiError = function (message) {
        var container = document.getElementById("apiErrorContainer");
        if (!container) {
            container = document.createElement("div");
            container.id = "apiErrorContainer";
            container.style.cssText = "background:#fef2f2;border:1px solid #fecaca;border-radius:8px;padding:16px 20px;margin:16px;display:flex;align-items:center;gap:12px;color:#991b1b;font-size:14px;";
            var mainBody = document.getElementById("mainBody");
            if (mainBody && mainBody.firstChild) {
                mainBody.insertBefore(container, mainBody.firstChild);
            } else if (mainBody) {
                mainBody.appendChild(container);
            }
        }
        container.innerHTML = '<span style="font-size:20px;">\u26A0\uFE0F</span><span>' + message.replace(/</g, "&lt;").replace(/>/g, "&gt;") + '</span>';
        container.style.display = "flex";
        setTimeout(function () { container.style.display = "none"; }, 8000);
    };

    window.APP.hideError = function () {
        var overlay = document.getElementById("appErrorOverlay");
        if (overlay) overlay.style.display = "none";
    };

    setupJQueryAuth();

    if (typeof document !== "undefined") {
        document.addEventListener("DOMContentLoaded", function () {
            setupJQueryAuth();
            var skipStartupAuthCheck = window.APP && window.APP.skipStartupAuthCheck === true;
            if (!window.location.pathname.toLowerCase().includes("/login") && !skipStartupAuthCheck) {
                initializeAuth({ redirectOnFailure: true }).catch(function () { });
            }
        });
    }

    (function pollJQuery() {
        if (typeof jQuery !== "undefined") {
            setupJQueryAuth();
        } else {
            setTimeout(pollJQuery, 50);
        }
    })();
})();
