
TARGET = LogisticRegression.exe
all: $(TARGET)

NAIAD_ROOT = ../..

Microsoft.Research.Naiad.dll:
	cd $(NAIAD_ROOT); sh build_mono.sh; cd -;
	cp $(NAIAD_ROOT)/Naiad/bin/Debug/Microsoft.Research.Naiad.dll . 

Microsoft.Research.Naiad.Lindi.dll:
	cd $(NAIAD_ROOT); sh build_mono.sh; cd -;
	cp $(NAIAD_ROOT)/Examples/bin/Debug/Microsoft.Research.Naiad.Lindi.dll . 

Microsoft.Research.Naiad.GraphLINQ.dll:
	cd $(NAIAD_ROOT); sh build_mono.sh; cd -;
	cp $(NAIAD_ROOT)/Examples/bin/Debug/Microsoft.Research.Naiad.GraphLINQ.dll . 


$(TARGET): LogisticRegression.cs Microsoft.Research.Naiad.dll Microsoft.Research.Naiad.Lindi.dll Microsoft.Research.Naiad.GraphLINQ.dll
	dmcs LogisticRegression.cs /reference:Microsoft.Research.Naiad.dll /reference:Microsoft.Research.Naiad.Lindi.dll  /reference:Microsoft.Research.Naiad.GraphLINQ.dll

run: $(TARGET)
	mono LogisticRegression.exe -t 2 10 10 4 1 1  

run-dist: $(TARGET)
	xterm -hold -e mono LogisticRegression.exe -n 2 --local -p 0 -t 2 --inlineserializer 10 10 8 1 2 &
	sleep 1
	xterm -hold -e mono LogisticRegression.exe -n 2 --local -p 1 -t 2 --inlineserializer 10 10 8 1 2 &

clean:
	\rm -f $(TARGET)

clean-all: clean
	\rm -f *.dll
