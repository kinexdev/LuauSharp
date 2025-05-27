-- Userdata
local test: example = example.new(math.random(1, 100))
test:print()

-- Variables
local num: number = 10
num -= 5

-- Calling print
print("hello world!")
print(num)

-- Getting a variable from userdata.
print("number of userdata test is " .. tostring(test.number))

-- Modifying a variable from userdata.
local randomNumber: number = math.random(1, 100)
test.number += randomNumber
print("added the value " .. tostring(randomNumber))
print("number of userdata test is " .. tostring(test.number))

test:print()