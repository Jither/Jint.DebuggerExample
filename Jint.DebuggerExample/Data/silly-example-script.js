const example = {
    regex: /[a-z0-9]/i,
    date: new Date(),
    boolean: true,
    string: "Here's a string\nwith new-line",
    number: 3.14159265359,
    bigint: 340282366920938463463374607431768211456n,
    null: null,
    undefined: undefined,
    symbol: Symbol("testsymbol"),
    numSymbol: Symbol(1),
    array: [],
    byteArray: new Uint8Array(5),
    intArray: new Int32Array(10),

    get myGetter()
    {
        return "A property getter value!";
    }
};

function testFunction(a, b)
{
    let x = arguments.length;
    const arr = [];
    debugger;
    while (true)
    {
        arr.push(x);
        if (x >= 500)
        {
            break;
        }
        x++;
    }
}

for (let i = 0; i < 1000; i++)
{
    example.array.push(i);
}

testFunction(5, "Hello Jint!");