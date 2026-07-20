(self.webpackChunk_N_E = self.webpackChunk_N_E || []).push([[8224], {
    82501: function (e, n, t) {
        (window.__NEXT_P = window.__NEXT_P || []).push(["/guide/plugins/trigger", function () {
            return t(22976)
        }
        ])
    },
    64236: function (e, n, t) {
        "use strict";
        t.r(n),
            t.d(n, {
                Demo: function () {
                    return p
                }
            });
        var a = t(85893)
            , r = t(67294)
            , i = t(41664)
            , l = t(13145)
            , d = t(72478)
            , o = t(15251)
            , s = function () {
                return (0,
                    a.jsx)("svg", {
                        viewBox: "0 0 24 24",
                        height: "24",
                        width: "24",
                        xmlns: "http://www.w3.org/2000/svg",
                        children: (0,
                            a.jsxs)("g", {
                                transform: "matrix(1,0,0,1,0,0)",
                                children: [(0,
                                    a.jsx)("path", {
                                        d: "M11.736,5a1,1,0,0,1-.894-.553L9.894,2.553A1,1,0,0,0,9,2H1.5a1,1,0,0,0-1,1V21a1,1,0,0,0,1,1h21a1,1,0,0,0,1-1V6a1,1,0,0,0-1-1Z",
                                        fill: "none",
                                        stroke: "currentColor",
                                        strokeLinecap: "round",
                                        strokeLinejoin: "round"
                                    }), (0,
                                        a.jsx)("path", {
                                            d: "M14.5 9.5L18 13 14.5 16.5",
                                            fill: "none",
                                            stroke: "currentColor",
                                            strokeLinecap: "round",
                                            strokeLinejoin: "round"
                                        }), (0,
                                            a.jsx)("path", {
                                                d: "M9.5 9.5L6 13 9.5 16.5",
                                                fill: "none",
                                                stroke: "currentColor",
                                                strokeLinecap: "round",
                                                strokeLinejoin: "round"
                                            })]
                            })
                    })
            }
            , m = function () {
                return (0,
                    a.jsx)("svg", {
                        viewBox: "0 0 24 24",
                        height: "24",
                        width: "24",
                        xmlns: "http://www.w3.org/2000/svg",
                        children: (0,
                            a.jsxs)("g", {
                                transform: "matrix(1,0,0,1,0,0)",
                                children: [(0,
                                    a.jsx)("path", {
                                        d: "M13.5,14a2,2,0,0,0,2,2h5a3.008,3.008,0,0,0,3-3V11a3.009,3.009,0,0,0-3-3h-5a2,2,0,0,0-2,2",
                                        fill: "none",
                                        stroke: "currentColor",
                                        strokeLinecap: "round",
                                        strokeLinejoin: "round"
                                    }), (0,
                                        a.jsx)("path", {
                                            d: "M10.5,14a2,2,0,0,1-2,2h-5a3.008,3.008,0,0,1-3-3V11a3.009,3.009,0,0,1,3-3h5a2,2,0,0,1,2,2",
                                            fill: "none",
                                            stroke: "currentColor",
                                            strokeLinecap: "round",
                                            strokeLinejoin: "round"
                                        }), (0,
                                            a.jsx)("path", {
                                                d: "M6.5 11.999L17.5 11.999",
                                                fill: "none",
                                                stroke: "currentColor",
                                                strokeLinecap: "round",
                                                strokeLinejoin: "round"
                                            })]
                            })
                    })
            }
            , u = t(70505);
        function c(e, n) {
            return function (e) {
                if (Array.isArray(e))
                    return e
            }(e) || function (e, n) {
                var t = []
                    , a = !0
                    , r = !1
                    , i = void 0;
                try {
                    for (var l, d = e[Symbol.iterator](); !(a = (l = d.next()).done) && (t.push(l.value),
                        !n || t.length !== n); a = !0)
                        ;
                } catch (o) {
                    r = !0,
                        i = o
                } finally {
                    try {
                        a || null == d.return || d.return()
                    } finally {
                        if (r)
                            throw i
                    }
                }
                return t
            }(e, n) || function () {
                throw new TypeError("Invalid attempt to destructure non-iterable instance")
            }()
        }
        var p = function (e) {
            var n = e.setFrameHeight
                , t = e.title
                , p = void 0 === t ? "" : t
                , h = e.url
                , g = h.replace("/demo/", "http://web.archive.org/web/20220809053949/https://github.com/form-validation/examples/blob/master/")
                , x = c(r.useState(""), 2)
                , f = x[0]
                , b = x[1]
                , v = c(r.useState(!1), 2)
                , N = v[0]
                , j = v[1];
            return (0,
                a.jsxs)(a.Fragment, {
                    children: [(0,
                        a.jsx)(l.Spacer, {
                            size: "small"
                        }), (0,
                            a.jsx)(l.Window, {
                                title: p,
                                children: (0,
                                    a.jsxs)("div", {
                                        className: "demo",
                                        children: [(0,
                                            a.jsx)("div", {
                                                className: "demo__frame",
                                                children: (0,
                                                    a.jsx)(o.Z, {
                                                        setFrameHeight: n,
                                                        url: h
                                                    })
                                            }), (0,
                                                a.jsxs)("div", {
                                                    className: "demo__toolbar",
                                                    children: [(0,
                                                        a.jsx)("button", {
                                                            className: "demo__button",
                                                            type: "button",
                                                            onClick: function () {
                                                                j(!N),
                                                                    f || fetch(h).then((function (e) {
                                                                        return e.text()
                                                                    }
                                                                    )).then((function (e) {
                                                                        return b(e)
                                                                    }
                                                                    ))
                                                            },
                                                            children: (0,
                                                                a.jsx)(s, {})
                                                        }), (0,
                                                            a.jsx)(i.default, {
                                                                href: g,
                                                                children: (0,
                                                                    a.jsx)("a", {
                                                                        className: "demo__button",
                                                                        target: "blank",
                                                                        rel: "noopener noreferrer",
                                                                        children: (0,
                                                                            a.jsx)(m, {})
                                                                    })
                                                            })]
                                                }), (0,
                                                    a.jsxs)("div", {
                                                        className: (0,
                                                            d.A)({
                                                                demo__code: !0,
                                                                "demo__code--opened": N
                                                            }),
                                                        children: [!f && (0,
                                                            a.jsx)("div", {
                                                                className: "demo__code-loader",
                                                                children: (0,
                                                                    a.jsx)(l.Spinner, {})
                                                            }), f && (0,
                                                                a.jsx)("div", {
                                                                    className: "demo__code-body",
                                                                    children: (0,
                                                                        a.jsx)(u.Z, {
                                                                            className: "lang-html",
                                                                            children: f
                                                                        })
                                                                })]
                                                    })]
                                    })
                            }), (0,
                                a.jsx)(l.Spacer, {
                                    size: "small"
                                })]
                })
        }
    },
    22976: function (e, n, t) {
        "use strict";
        t.r(n),
            t.d(n, {
                default: function () {
                    return m
                }
            });
        t(67294);
        var a = t(3905)
            , r = t(13145)
            , i = t(64236)
            , l = t(83706);
        function d(e, n) {
            if (null == e)
                return {};
            var t, a, r = function (e, n) {
                if (null == e)
                    return {};
                var t, a, r = {}, i = Object.keys(e);
                for (a = 0; a < i.length; a++)
                    t = i[a],
                        n.indexOf(t) >= 0 || (r[t] = e[t]);
                return r
            }(e, n);
            if (Object.getOwnPropertySymbols) {
                var i = Object.getOwnPropertySymbols(e);
                for (a = 0; a < i.length; a++)
                    t = i[a],
                        n.indexOf(t) >= 0 || Object.prototype.propertyIsEnumerable.call(e, t) && (r[t] = e[t])
            }
            return r
        }
        var o = {}
            , s = function (e) {
                var n = e.children;
                return (0,
                    a.mdx)(l.Z, {
                        title: "Trigger plugin",
                        secondaryTitle: "Indicate the events which the validation will be executed when these events are triggered"
                    }, n)
            };
        function m(e) {
            var n = e.components
                , t = d(e, ["components"]);
            return (0,
                a.mdx)(s, Object.assign({}, o, t, {
                    components: n,
                    mdxType: "MDXLayout"
                }), (0,
                    a.mdx)("h3", null, "Usage"), (0,
                        a.mdx)("p", null, "By default, the field will be validated automatically when the ", (0,
                            a.mdx)("inlineCode", {
                                parentName: "p"
                            }, "input"), " event is triggered. This plugin allows to set the specific events for given field as fllowing:"), (0,
                                a.mdx)("pre", null, (0,
                                    a.mdx)("code", Object.assign({
                                        parentName: "pre"
                                    }, {
                                        className: "language-html"
                                    }), '<html>\n    <head>\n        <link rel="stylesheet" href="/vendors/formvalidation/dist/css/formValidation.min.css" />\n    </head>\n    <body>\n        <form id="demoForm" method="POST">...</form>\n\n        <script src="http://web.archive.org/web/20220809053949/https://cdnjs.cloudflare.com/ajax/libs/es6-shim/0.35.3/es6-shim.min.js"><\/script>\n        <script src="/vendors/formvalidation/dist/js/FormValidation.min.js"><\/script>\n\n        <script>\n            document.addEventListener(\'DOMContentLoaded\', function(e) {\n                FormValidation.formValidation(\n                    document.getElementById(\'demoForm\'),\n                    {\n                        fields: {\n                            ...\n                        },\n                        plugins: {\n                            trigger: new FormValidation.plugins.Trigger({\n                                event: ...,\n                                threshold: ...,\n                                delay: ...,\n                            }),\n                            ...\n                        },\n                    }\n                );\n            });\n        <\/script>\n    </body>\n</html>\n')), (0,
                                        a.mdx)("p", null, "The sample code above assumes that the FormValidation files are placed inside the ", (0,
                                            a.mdx)("inlineCode", {
                                                parentName: "p"
                                            }, "vendors"), " directory. You might need to change the path depending on where you place them on the server."), (0,
                                                a.mdx)("h3", null, "Options"), (0,
                                                    a.mdx)("table", null, (0,
                                                        a.mdx)("thead", {
                                                            parentName: "table"
                                                        }, (0,
                                                            a.mdx)("tr", {
                                                                parentName: "thead"
                                                            }, (0,
                                                                a.mdx)("th", Object.assign({
                                                                    parentName: "tr"
                                                                }, {
                                                                    align: null
                                                                }), "Option"), (0,
                                                                    a.mdx)("th", Object.assign({
                                                                        parentName: "tr"
                                                                    }, {
                                                                        align: null
                                                                    }), "Type"), (0,
                                                                        a.mdx)("th", Object.assign({
                                                                            parentName: "tr"
                                                                        }, {
                                                                            align: null
                                                                        }), "Description"))), (0,
                                                                            a.mdx)("tbody", {
                                                                                parentName: "table"
                                                                            }, (0,
                                                                                a.mdx)("tr", {
                                                                                    parentName: "tbody"
                                                                                }, (0,
                                                                                    a.mdx)("td", Object.assign({
                                                                                        parentName: "tr"
                                                                                    }, {
                                                                                        align: null
                                                                                    }), (0,
                                                                                        a.mdx)("inlineCode", {
                                                                                            parentName: "td"
                                                                                        }, "event")), (0,
                                                                                            a.mdx)("td", Object.assign({
                                                                                                parentName: "tr"
                                                                                            }, {
                                                                                                align: null
                                                                                            }), (0,
                                                                                                a.mdx)("inlineCode", {
                                                                                                    parentName: "td"
                                                                                                }, "String"), " or ", (0,
                                                                                                    a.mdx)("inlineCode", {
                                                                                                        parentName: "td"
                                                                                                    }, "Object")), (0,
                                                                                                        a.mdx)("td", Object.assign({
                                                                                                            parentName: "tr"
                                                                                                        }, {
                                                                                                            align: null
                                                                                                        }), "Define the events")))), (0,
                                                                                                            a.mdx)("p", null, "The ", (0,
                                                                                                                a.mdx)("inlineCode", {
                                                                                                                    parentName: "p"
                                                                                                                }, "event"), " option can be one of the following formats:"), (0,
                                                                                                                    a.mdx)("pre", null, (0,
                                                                                                                        a.mdx)("code", Object.assign({
                                                                                                                            parentName: "pre"
                                                                                                                        }, {
                                                                                                                            className: "language-js"
                                                                                                                        }), "// Validate fields when the blur events are triggered\nevent: 'blur',\n\n// If you need multiple events are fired,\n// then separate them by a space\nevent: 'blur change',\n\n// We can indicate different events for\n// each particular field\nevent: {\n    fullName: 'blur',\n    email: 'change',\n},\n\n// If we do not want the field to be validated\n// automatically, set the associate value to false\nevent: {\n    // The field is only validated when we click the\n    // submit button of form\n    // (if the SubmitButton plugin is used)\n    email: false,\n},\n")), (0,
                                                                                                                            a.mdx)("table", null, (0,
                                                                                                                                a.mdx)("thead", {
                                                                                                                                    parentName: "table"
                                                                                                                                }, (0,
                                                                                                                                    a.mdx)("tr", {
                                                                                                                                        parentName: "thead"
                                                                                                                                    }, (0,
                                                                                                                                        a.mdx)("th", Object.assign({
                                                                                                                                            parentName: "tr"
                                                                                                                                        }, {
                                                                                                                                            align: null
                                                                                                                                        }), "Option"), (0,
                                                                                                                                            a.mdx)("th", Object.assign({
                                                                                                                                                parentName: "tr"
                                                                                                                                            }, {
                                                                                                                                                align: null
                                                                                                                                            }), "Type"), (0,
                                                                                                                                                a.mdx)("th", Object.assign({
                                                                                                                                                    parentName: "tr"
                                                                                                                                                }, {
                                                                                                                                                    align: null
                                                                                                                                                }), "Description"))), (0,
                                                                                                                                                    a.mdx)("tbody", {
                                                                                                                                                        parentName: "table"
                                                                                                                                                    }, (0,
                                                                                                                                                        a.mdx)("tr", {
                                                                                                                                                            parentName: "tbody"
                                                                                                                                                        }, (0,
                                                                                                                                                            a.mdx)("td", Object.assign({
                                                                                                                                                                parentName: "tr"
                                                                                                                                                            }, {
                                                                                                                                                                align: null
                                                                                                                                                            }), (0,
                                                                                                                                                                a.mdx)("inlineCode", {
                                                                                                                                                                    parentName: "td"
                                                                                                                                                                }, "threshold")), (0,
                                                                                                                                                                    a.mdx)("td", Object.assign({
                                                                                                                                                                        parentName: "tr"
                                                                                                                                                                    }, {
                                                                                                                                                                        align: null
                                                                                                                                                                    }), (0,
                                                                                                                                                                        a.mdx)("inlineCode", {
                                                                                                                                                                            parentName: "td"
                                                                                                                                                                        }, "Number"), " or ", (0,
                                                                                                                                                                            a.mdx)("inlineCode", {
                                                                                                                                                                                parentName: "td"
                                                                                                                                                                            }, "Object")), (0,
                                                                                                                                                                                a.mdx)("td", Object.assign({
                                                                                                                                                                                    parentName: "tr"
                                                                                                                                                                                }, {
                                                                                                                                                                                    align: null
                                                                                                                                                                                }), "Only perform the validation if the field value exceed this number of characters. By default, it's set to 0. This option does not effect to the elements which user cannot type, such as radio, checkbox, select one.")))), (0,
                                                                                                                                                                                    a.mdx)("pre", null, (0,
                                                                                                                                                                                        a.mdx)("code", Object.assign({
                                                                                                                                                                                            parentName: "pre"
                                                                                                                                                                                        }, {
                                                                                                                                                                                            className: "language-js"
                                                                                                                                                                                        }), "// Validate fields when they have at least 5 characters\nthreshold: 5,\n\n// We can indicate different threshold for\n// each particular field\nthreshold: {\n    fullName: 15,\n    email: 10,\n},\n")), (0,
                                                                                                                                                                                            a.mdx)("table", null, (0,
                                                                                                                                                                                                a.mdx)("thead", {
                                                                                                                                                                                                    parentName: "table"
                                                                                                                                                                                                }, (0,
                                                                                                                                                                                                    a.mdx)("tr", {
                                                                                                                                                                                                        parentName: "thead"
                                                                                                                                                                                                    }, (0,
                                                                                                                                                                                                        a.mdx)("th", Object.assign({
                                                                                                                                                                                                            parentName: "tr"
                                                                                                                                                                                                        }, {
                                                                                                                                                                                                            align: null
                                                                                                                                                                                                        }), "Option"), (0,
                                                                                                                                                                                                            a.mdx)("th", Object.assign({
                                                                                                                                                                                                                parentName: "tr"
                                                                                                                                                                                                            }, {
                                                                                                                                                                                                                align: null
                                                                                                                                                                                                            }), "Type"), (0,
                                                                                                                                                                                                                a.mdx)("th", Object.assign({
                                                                                                                                                                                                                    parentName: "tr"
                                                                                                                                                                                                                }, {
                                                                                                                                                                                                                    align: null
                                                                                                                                                                                                                }), "Description"))), (0,
                                                                                                                                                                                                                    a.mdx)("tbody", {
                                                                                                                                                                                                                        parentName: "table"
                                                                                                                                                                                                                    }, (0,
                                                                                                                                                                                                                        a.mdx)("tr", {
                                                                                                                                                                                                                            parentName: "tbody"
                                                                                                                                                                                                                        }, (0,
                                                                                                                                                                                                                            a.mdx)("td", Object.assign({
                                                                                                                                                                                                                                parentName: "tr"
                                                                                                                                                                                                                            }, {
                                                                                                                                                                                                                                align: null
                                                                                                                                                                                                                            }), (0,
                                                                                                                                                                                                                                a.mdx)("inlineCode", {
                                                                                                                                                                                                                                    parentName: "td"
                                                                                                                                                                                                                                }, "delay")), (0,
                                                                                                                                                                                                                                    a.mdx)("td", Object.assign({
                                                                                                                                                                                                                                        parentName: "tr"
                                                                                                                                                                                                                                    }, {
                                                                                                                                                                                                                                        align: null
                                                                                                                                                                                                                                    }), (0,
                                                                                                                                                                                                                                        a.mdx)("inlineCode", {
                                                                                                                                                                                                                                            parentName: "td"
                                                                                                                                                                                                                                        }, "Number"), " or ", (0,
                                                                                                                                                                                                                                            a.mdx)("inlineCode", {
                                                                                                                                                                                                                                                parentName: "td"
                                                                                                                                                                                                                                            }, "Object")), (0,
                                                                                                                                                                                                                                                a.mdx)("td", Object.assign({
                                                                                                                                                                                                                                                    parentName: "tr"
                                                                                                                                                                                                                                                }, {
                                                                                                                                                                                                                                                    align: null
                                                                                                                                                                                                                                                }), "Pending validation for a given number of seconds after user stops filling in the field. By default, it's set to 0. It's really useful if the field needs to perform validation in server side, such as the ", (0,
                                                                                                                                                                                                                                                    a.mdx)("a", Object.assign({
                                                                                                                                                                                                                                                        parentName: "td"
                                                                                                                                                                                                                                                    }, {
                                                                                                                                                                                                                                                        href: "/guide/validators/remote"
                                                                                                                                                                                                                                                    }), "remote validator"))))), (0,
                                                                                                                                                                                                                                                        a.mdx)("pre", null, (0,
                                                                                                                                                                                                                                                            a.mdx)("code", Object.assign({
                                                                                                                                                                                                                                                                parentName: "pre"
                                                                                                                                                                                                                                                            }, {
                                                                                                                                                                                                                                                                className: "language-js"
                                                                                                                                                                                                                                                            }), "// Validate fields after 5 seconds from the moment\n// user stops filling in the field\ndelay: 5,\n\n// We can indicate different delay for\n// each particular field\ndelay: {\n    fullName: 0,\n    username: 5,\n},\n")), (0,
                                                                                                                                                                                                                                                                a.mdx)("h3", null, "Basic example"), (0,
                                                                                                                                                                                                                                                                    a.mdx)("p", null, "In the following form, the ", (0,
                                                                                                                                                                                                                                                                        a.mdx)("em", {
                                                                                                                                                                                                                                                                            parentName: "p"
                                                                                                                                                                                                                                                                        }, "Title"), " field will be validated while user type any character (", (0,
                                                                                                                                                                                                                                                                            a.mdx)("inlineCode", {
                                                                                                                                                                                                                                                                                parentName: "p"
                                                                                                                                                                                                                                                                            }, 'trigger="keyup"'), "). The ", (0,
                                                                                                                                                                                                                                                                                a.mdx)("em", {
                                                                                                                                                                                                                                                                                    parentName: "p"
                                                                                                                                                                                                                                                                                }, "Summary"), " field will be validated when user lose the focus (", (0,
                                                                                                                                                                                                                                                                                    a.mdx)("inlineCode", {
                                                                                                                                                                                                                                                                                        parentName: "p"
                                                                                                                                                                                                                                                                                    }, 'trigger="blur"'), ")."), (0,
                                                                                                                                                                                                                                                                                        a.mdx)(i.Demo, {
                                                                                                                                                                                                                                                                                            title: "Trigger plugin",
                                                                                                                                                                                                                                                                                            url: "/demo/plugin-trigger/basic.html",
                                                                                                                                                                                                                                                                                            mdxType: "Demo"
                                                                                                                                                                                                                                                                                        }), (0,
                                                                                                                                                                                                                                                                                            a.mdx)("h3", null, "See also"), (0,
                                                                                                                                                                                                                                                                                                a.mdx)("ul", null, (0,
                                                                                                                                                                                                                                                                                                    a.mdx)("li", {
                                                                                                                                                                                                                                                                                                        parentName: "ul"
                                                                                                                                                                                                                                                                                                    }, (0,
                                                                                                                                                                                                                                                                                                        a.mdx)("a", Object.assign({
                                                                                                                                                                                                                                                                                                            parentName: "li"
                                                                                                                                                                                                                                                                                                        }, {
                                                                                                                                                                                                                                                                                                            href: "/guide/examples/pending-validation-for-a-given-number-of-seconds"
                                                                                                                                                                                                                                                                                                        }), "Pending validation for a given number of seconds")), (0,
                                                                                                                                                                                                                                                                                                            a.mdx)("li", {
                                                                                                                                                                                                                                                                                                                parentName: "ul"
                                                                                                                                                                                                                                                                                                            }, (0,
                                                                                                                                                                                                                                                                                                                a.mdx)("a", Object.assign({
                                                                                                                                                                                                                                                                                                                    parentName: "li"
                                                                                                                                                                                                                                                                                                                }, {
                                                                                                                                                                                                                                                                                                                    href: "/guide/examples/performing-validation-if-field-value-exceed-given-number-of-characters"
                                                                                                                                                                                                                                                                                                                }), "Performing validation if field value exceed given number of characters"))), (0,
                                                                                                                                                                                                                                                                                                                    a.mdx)("h3", null, "Changelog"), (0,
                                                                                                                                                                                                                                                                                                                        a.mdx)(r.Accordion, {
                                                                                                                                                                                                                                                                                                                            title: "v1.6.0",
                                                                                                                                                                                                                                                                                                                            mdxType: "Accordion"
                                                                                                                                                                                                                                                                                                                        }, (0,
                                                                                                                                                                                                                                                                                                                            a.mdx)("ul", null, (0,
                                                                                                                                                                                                                                                                                                                                a.mdx)("li", {
                                                                                                                                                                                                                                                                                                                                    parentName: "ul"
                                                                                                                                                                                                                                                                                                                                }, "Added a new filter named ", (0,
                                                                                                                                                                                                                                                                                                                                    a.mdx)("inlineCode", {
                                                                                                                                                                                                                                                                                                                                        parentName: "li"
                                                                                                                                                                                                                                                                                                                                    }, "plugins-trigger-should-validate"), ". We can use it to determine the field is validated automatically when the value is changed or not."))), (0,
                                                                                                                                                                                                                                                                                                                                        a.mdx)(r.Accordion, {
                                                                                                                                                                                                                                                                                                                                            title: "v1.1.0",
                                                                                                                                                                                                                                                                                                                                            mdxType: "Accordion"
                                                                                                                                                                                                                                                                                                                                        }, (0,
                                                                                                                                                                                                                                                                                                                                            a.mdx)("ul", null, (0,
                                                                                                                                                                                                                                                                                                                                                a.mdx)("li", {
                                                                                                                                                                                                                                                                                                                                                    parentName: "ul"
                                                                                                                                                                                                                                                                                                                                                }, "Added new ", (0,
                                                                                                                                                                                                                                                                                                                                                    a.mdx)("inlineCode", {
                                                                                                                                                                                                                                                                                                                                                        parentName: "li"
                                                                                                                                                                                                                                                                                                                                                    }, "delay"), " option"))), (0,
                                                                                                                                                                                                                                                                                                                                                        a.mdx)(r.Accordion, {
                                                                                                                                                                                                                                                                                                                                                            title: "v1.0.0",
                                                                                                                                                                                                                                                                                                                                                            mdxType: "Accordion"
                                                                                                                                                                                                                                                                                                                                                        }, (0,
                                                                                                                                                                                                                                                                                                                                                            a.mdx)("ul", null, (0,
                                                                                                                                                                                                                                                                                                                                                                a.mdx)("li", {
                                                                                                                                                                                                                                                                                                                                                                    parentName: "ul"
                                                                                                                                                                                                                                                                                                                                                                }, "First release"))), (0,
                                                                                                                                                                                                                                                                                                                                                                    a.mdx)(r.Spacer, {
                                                                                                                                                                                                                                                                                                                                                                        size: "medium",
                                                                                                                                                                                                                                                                                                                                                                        mdxType: "Spacer"
                                                                                                                                                                                                                                                                                                                                                                    }), (0,
                                                                                                                                                                                                                                                                                                                                                                        a.mdx)(r.Pagination, {
                                                                                                                                                                                                                                                                                                                                                                            mdxType: "Pagination"
                                                                                                                                                                                                                                                                                                                                                                        }, (0,
                                                                                                                                                                                                                                                                                                                                                                            a.mdx)(r.PrevPagination, {
                                                                                                                                                                                                                                                                                                                                                                                href: "/guide/plugins/transformer",
                                                                                                                                                                                                                                                                                                                                                                                mdxType: "PrevPagination"
                                                                                                                                                                                                                                                                                                                                                                            }, "Transformer plugin"), (0,
                                                                                                                                                                                                                                                                                                                                                                                a.mdx)(r.NextPagination, {
                                                                                                                                                                                                                                                                                                                                                                                    href: "/guide/plugins/turret",
                                                                                                                                                                                                                                                                                                                                                                                    mdxType: "NextPagination"
                                                                                                                                                                                                                                                                                                                                                                                }, "Turret plugin")))
        }
        m.isMDXComponent = !0
    }
}, function (e) {
    e.O(0, [3582, 3706, 9774, 2888, 179], (function () {
        return n = 82501,
            e(e.s = n);
        var n
    }
    ));
    var n = e.O();
    _N_E = n
}
]);
