window.AppEvents = (() => {

    function create() {

        const listeners = {};

        function on(event, callback) {

            if (!listeners[event]) {
                listeners[event] = [];
            }

            listeners[event].push(callback);

            return this;
        }

        function off(event, callback) {

            if (!listeners[event]) {
                return this;
            }

            if (!callback) {
                delete listeners[event];
                return this;
            }

            listeners[event] =
                listeners[event].filter(x => x !== callback);

            return this;
        }

        async function emit(event, ...args) {

            if (!listeners[event]) {
                return;
            }

            for (const callback of listeners[event]) {
                await callback(...args);
            }

        }

        function once(event, callback) {

            const wrapper = async (...args) => {

                off(event, wrapper);

                await callback(...args);

            };

            on(event, wrapper);

            return this;
        }

        return {

            on,

            off,

            once,

            emit

        };

    }

    return {

        create

    };

})();