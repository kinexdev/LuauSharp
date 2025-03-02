-- this is to show off the GC
-- this is called every frame, it creates a new userdata with a random number and then calls a method called 'print' using that userdata reference
-- since there is no reference of the 'test' userdata, it will get cleaned up by the incremental GC which will then get freed up by the C# GC.
function _tick()
    local test: Test = Test.New(math.random(1, 100))
    test:print()
end