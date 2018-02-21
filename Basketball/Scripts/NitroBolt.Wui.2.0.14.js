var ContainerSynchronizer = (function () {
    function ContainerSynchronizer(container, name, sync_refresh_period, id) {
        var _this = this;
        if (container === void 0) { container = null; }
        if (name === void 0) { name = null; }
        if (sync_refresh_period === void 0) { sync_refresh_period = 10 * 1000; }
        if (id === void 0) { id = null; }
        this.cycle = 0;
        this.is_need_update = false;
        this.is_updating = false;
        this.commands = [];
        this.container = container != null ? $(container) : $('body');
        this.container_name = name;
        ContainerSynchronizer.sync_refresh_period = sync_refresh_period;
        this.id = id != null ? id : Math.random().toString();
        ContainerSynchronizer.update_tick = 0;
        window.setTimeout(function timeout_tick() {
          try {
            var tick = new Date().getTime();
            if (ContainerSynchronizer.update_tick + 10000 > tick)
              return;
            ContainerSynchronizer.update_tick = tick;
            _this.update_all();
          }
          finally {
            window.setTimeout(timeout_tick, ContainerSynchronizer.sync_refresh_period);
          }
        }, ContainerSynchronizer.sync_refresh_period);
        window.setInterval(function () {
            if (_this.is_need_update) {
                _this.is_need_update = false;
                _this.update_all();
            }
        }, 50);
        this.update_all();
    }
    ContainerSynchronizer.prototype.server_element_event = function (_element, event, data) {
        var element = $(_element);
        var e = null;
        if (event != null) {
            e = {};
            var eventProps = ContainerSynchronizer.eventProps;
            for (var i = 0; i < eventProps.length; ++i)
                e[eventProps[i]] = event[eventProps[i]];
        }
        var element_data = element.data();
        var result_data = {};
        if (element_data.container != null) {
            var container = null;
            var parents = element.parents();
            for (var i = 0; i < parents.length; ++i) {
                if ($(parents[i]).data().name == element_data.container) {
                    container = $(parents[i]);
                    break;
                }
            }
            if (container != null) {
                result_data = $.extend(result_data, container.data());
                var childs = $.merge(container.find('input'), container.find('select'));
                childs = $.merge(childs, container.find('textarea'));
                for (var i = 0; i < childs.length; ++i) {
                    var child = $(childs[i]);
                    if (child.data().name != null) {
                        if (!child.is(':radio') || child.is(':checked'))
                            result_data[child.data().name] = this.element_value(child);
                    }
                }
            }
        }
        this.server_event({ value: this.element_value(element), checked: element.is(':checked'), data: $.extend(result_data, element_data, data), event: e });
    };
    ContainerSynchronizer.prototype.element_value = function (element) {
        if (element.is(':checkbox'))
            return element.is(':checked');
        if (element.is(':radio'))
            return element.is(':checked') ? element.val() : null;
        return element.val();
    };
    ContainerSynchronizer.prototype.find_element = function (current, path) {
        var len = path.length;
        for (var i = 0; i < len; ++i) {
            if (!current)
                return null;
            var pentry = path[i];
            if (pentry.kind == 'element') {
                var childs = current.children;
                current = pentry.index < childs.length ? childs[pentry.index] : null;
            }
        }
        return current;
    };
    ContainerSynchronizer.prototype.is_event_name = function (name) {
        if (!(name.substring(0, 2) === 'on'))
            return false;
        switch (name) {
            case 'onclick':
            case 'ondblclick':
            case 'onmousedown':
            case 'onmousemove':
            case 'onmouseover':
            case 'onmouseout':
            case 'onmouseup':
            case 'onkeydown':
            case 'onkeypress':
            case 'onkeyup':
            case 'onblur':
            case 'onchange':
            case 'onfocus':
            case 'onreset':
            case 'onselect':
            case 'onsubmit':
            case 'onabort':
            case 'onerror':
            case 'onload':
            case 'onresize':
            case 'onscroll':
            case 'onunload':
                return true;
        }
        return false;
    };
    ContainerSynchronizer.prototype.event_on = function (element, event, value) {
        var _this = this;
        if (value != null) {
            element.on(event, function (e) {
                if (value.substr(0, 2) == ';;') {
                    var res = function (sync, e) { return eval(value); }.apply(element.get(0), [_this, e]);
                    if (typeof (res) == 'boolean')
                        return res;
                }
                else {
                    var res = function () { return eval(value); }.apply(element.get(0));
                    if (typeof (res) == 'boolean')
                        return res;
                    _this.server_element_event(element, e);
                }
            });
        }
    };
    ContainerSynchronizer.prototype.set_element = function (element, desc) {
        if (!desc || !element)
            return;
        var len = !desc.e ? 0 : desc.e.length;
        for (var i = 0; i < len; ++i) {
            element.append(this.create_element(desc.e[i]));
        }
        var len = !desc.a ? 0 : desc.a.length;
        for (var i = 0; i < len; ++i) {
            if (this.is_event_name(desc.a[i].name)) {
                var event = desc.a[i].name.substring(2);
                var value = desc.a[i].value;
                element.off(event);
                this.event_on(element, event, value);
            }
            else if (desc.a[i].name.substring(0, 5) === 'data-') {
                element.data(desc.a[i].name.substring(5), desc.a[i].value);
            }
            else {
                element.attr(desc.a[i].name, desc.a[i].value);
            }
        }
        if (desc.t != null) {
            element.text(desc.t.value);
        }
        if (desc.h != null) {
            element.html(desc.h);
        }
    };
    ContainerSynchronizer.prototype.create_element = function (desc) {
        var element = $(desc.ns ? document.createElementNS(desc.ns, desc.name) : document.createElement(desc.name));
        var jsInit = null;
        for (var i = 0; i < (!desc.a ? 0 : desc.a.length); ++i) {
            if (desc.a[i].name == 'js-init')
                jsInit = desc.a[i].value;
        }
        if (jsInit != null) {
            !(function () { return eval(jsInit); }.apply(element.get(0)));
        }
        this.set_element(element, desc);
        return element;
    };
    ContainerSynchronizer.prototype.change_element = function (current, cmd, desc) {
        if (!current)
            return;
        switch (cmd) {
            case 'remove':
                current.remove();
                break;
            case 'clear':
                current.empty();
                break;
            case 'clear-all':
                current.empty();
                var attributes = $.map(current[0].attributes, function (item) { return item.name; });
                $.each(attributes, function (i, item) { return current.removeAttr(item); });
                break;
            case 'set':
                this.set_element(current, desc);
                break;
            case 'after':
                current.after(this.create_element(desc));
                break;
            case 'insert':
                current.prepend(this.create_element(desc));
                break;
            case 'js-update':
                !(function () { return eval(desc); }.apply(current.get(0)));
                break;
        }
    };
    ContainerSynchronizer.prototype.apply_commands = function (commands) {
        var len = commands.length;
        for (var i = 0; i < len; ++i) {
            var command = commands[i];
            this.change_element($(this.find_element(this.container.get(0), command.path)), command.cmd, command.value);
        }
    };
    ContainerSynchronizer.prototype.sync = function (data) {
        if (data.prev_cycle == this.cycle && !this.is_updating) {
            this.is_updating = true;
            try {
                this.apply_commands(data.updates);
                this.cycle = data.cycle;
                this.commands = this.commands.slice(data.processed_commands != null ? data.processed_commands : 0);
                if (this.commands.length > 0)
                    this.is_need_update = true;
            }
            finally {
                this.is_updating = false;
            }
        }
        else {
            this.is_need_update = true;
        }
    };
    ContainerSynchronizer.prototype.server_event = function (json) {
        var _this = this;
        this.commands.push((typeof json === 'string') ? JSON.parse(json) : json);
        //console.log(new Date().getTime(), 'server_event');
        $.post(this.js_path(), JSON.stringify({ 'frame': this.id, 'cycle': this.cycle, 'commands': this.commands }), function (data) { return _this.sync(data); }, 'json');
    };
    ContainerSynchronizer.prototype.update_all = function () {
      //console.log(new Date().getTime(), 'update_all');
        var _this = this;
        try {
            if (this.commands.length > 0) {
              $.post(this.js_path(), JSON.stringify({ 'frame': this.id, 'cycle': this.cycle, 'commands': this.commands }), function (data) { return _this.sync(data); }, 'json').always(function () { ContainerSynchronizer.update_tick = 0; });
            }
            else {
              $.post(this.js_path(), JSON.stringify({ 'frame': this.id, 'cycle': this.cycle }), function (data) { return _this.sync(data); }, 'json').always(function () { ContainerSynchronizer.update_tick = 0; });
            }
        }
        catch (e) {
            console.log(e);
        }
    };
    ContainerSynchronizer.prototype.js_path = function (query) {
        var path = this.container_name;
        if (path == null)
            path = window.location.href;
        if (query != null && query != '') {
            if (path.indexOf('?') < 0)
                path += '?' + query;
            else
                path += '&' + query;
        }
        return path;
    };
    ContainerSynchronizer.eventProps = ['type', 'bubbles', 'cancelable', 'eventPhase', 'timeStamp',
        'button', 'clientX', 'clientY', 'screenX', 'screenY',
        'keyIdentifier', 'keyLocation', 'keyCode', 'charCode', 'which',
        'altKey', 'ctrlKey', 'metaKey', 'shiftKey'
    ];
    return ContainerSynchronizer;
}());
var Command = (function () {
    function Command() {
    }
    return Command;
}());
var ElementDescription = (function () {
    function ElementDescription() {
    }
    return ElementDescription;
}());
var AttributeDescription = (function () {
    function AttributeDescription() {
    }
    return AttributeDescription;
}());
var TextDescription = (function () {
    function TextDescription() {
    }
    return TextDescription;
}());
var PathEntry = (function () {
    function PathEntry() {
    }
    return PathEntry;
}());
