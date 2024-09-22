mergeInto(LibraryManager.library, {
    WebLocalStorage_SetItem: function(keyPtr, valuePtr) {
        var key = UTF8ToString(keyPtr);
        var value = UTF8ToString(valuePtr);
        localStorage.setItem(key, value);
    },
    WebLocalStorage_GetItem: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        var value = localStorage.getItem(key);
        if (value === null) {
            return 0; // null указывает, что элемента нет
        } else {
            var bufferSize = lengthBytesUTF8(value) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(value, buffer, bufferSize);
            return buffer;
        }
    },
    WebLocalStorage_RemoveItem: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        localStorage.removeItem(key);
    },
    WebLocalStorage_HasKey: function(keyPtr) {
        var key = UTF8ToString(keyPtr);
        return localStorage.getItem(key) !== null;
    }
});
