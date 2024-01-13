class C
{
	#field = 123;
	get value()
	{
		debugger;
		return this.#field;
	}
}

const c = new C();
console.log(c.value);
console.warn(c.value);
console.error(c.value);
console.debug(c.value);
console.info(c.value);


