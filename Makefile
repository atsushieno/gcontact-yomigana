all: gcontact-yomigana.exe

gcontact-yomigana.exe: gcontact-yomigana.cs
	gmcs -debug -pkg:gdata-sharp-contacts gcontact-yomigana.cs

clean:
	rm -rf gcontact-yomigana.exe gcontact-yomigana.exe.mdb
