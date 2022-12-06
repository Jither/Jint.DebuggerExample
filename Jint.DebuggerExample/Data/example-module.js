class Test {
    init(prefix) {
        this.prefix = prefix;
    }

    run() {
        this.values = [];
        for (let i = 0; i < 10; i++) {
            this.values.push(this.prefix + i);
        }
    }
}

export default new Test();
