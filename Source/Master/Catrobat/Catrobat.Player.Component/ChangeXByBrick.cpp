#include "pch.h"
#include "ChangeXByBrick.h"
#include "Script.h"
#include "Object.h"
#include "Interpreter.h"

ChangeXByBrick::ChangeXByBrick(FormulaTree *offsetX, Script *parent) :
	Brick(TypeOfBrick::ChangeXByBrick, parent),
	m_offsetX(offsetX)
{
}

void ChangeXByBrick::Execute()
{
	float currentX, currentY;
	m_parent->GetParent()->GetPosition(currentX, currentY);
	m_parent->GetParent()->SetPosition(currentX + Interpreter::Instance()->EvaluateFormulaToFloat(m_offsetX, m_parent->GetParent()), currentY);
}