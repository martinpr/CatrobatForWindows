#pragma once

#include <map>
#include "Object.h"

class FormulaTree;

class Interpreter
{
private:
	static Interpreter *__instance;

public:
	Interpreter(void);
	~Interpreter(void);
	static Interpreter *Instance();

	double EvaluateFormula(FormulaTree *tree, Object *object);

	int EvaluateFormulaToInt(FormulaTree *tree, Object *object);
	float EvaluateFormulaToFloat(FormulaTree *tree, Object *object);
	bool EvaluateFormulaToBool(FormulaTree *tree, Object *object);

	void ReadAcceleration();

private:
	// Sensors
	Windows::Devices::Sensors::Accelerometer^ m_accelerometer;
    Windows::Devices::Sensors::AccelerometerReading^ m_accReading;

    // HelperFunctions
    int InterpretOperator(FormulaTree *tree, Object *object);
    float InterpretOperatorFloat(FormulaTree *tree, Object *object);
	bool InterpretOperatorBool(FormulaTree *tree, Object *object);
	bool InterpretFunctionBool(FormulaTree *tree, Object *object);
};

