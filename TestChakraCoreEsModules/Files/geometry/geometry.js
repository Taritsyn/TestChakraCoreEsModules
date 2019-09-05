import * as arithmetic from '../arithmetic/arithmetic.js';

export class Rectangle {
	constructor(width, height) {
		this.width = width;
		this.height = height;
	}

	get area() {
		return this.constructor.calculateArea(this.width, this.height);
	}

	static calculateArea(width, height) {
		return arithmetic.multiply(width, height);
	}
}

export class Square extends Rectangle {
	constructor(side) {
		super(side, side);
	}
}