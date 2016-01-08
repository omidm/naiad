
TARGET = FirstNaiadProgram.exe
all: $(TARGET)

NAIAD_ROOT = ../..

Microsoft.Research.Naiad.dll:
	cd $(NAIAD_ROOT); sh build_mono.sh; cd -;
	cp $(NAIAD_ROOT)/Naiad/bin/Debug/Microsoft.Research.Naiad.dll . 

$(TARGET): FirstNaiadProgram.cs Microsoft.Research.Naiad.dll
	dmcs FirstNaiadProgram.cs /reference:Microsoft.Research.Naiad.dll

run: $(TARGET)
	mono FirstNaiadProgram.exe

run-dist: $(TARGET)
	xterm -hold -e mono FirstNaiadProgram.exe -n 2 --local -p 0 &
	sleep 1
	xterm -hold -e mono FirstNaiadProgram.exe -n 2 --local -p 1

clean:
	\rm -f $(TARGET)

clean-all: clean
	\rm -f *.dll
