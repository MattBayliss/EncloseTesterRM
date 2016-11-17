# EncloseTester for HPE Records Manager

Just a personal experiment, whilst trying to debug an issue with creating Records in HPE Records Manager, and how the order seems to matter.
The subject of this test is a RecordType which has the Enclosed field defaulted to ticked, or "yes". When enclosed is ticked, the Assignee becomes the 
Record's Container.

The bug being tested is when a user tries to untick Enclosed, and set their own Assignee, those settings appear to be ignored, and the Enclosed remains
ticked. I suspected that the order in which the fields are applied to the record mattered, and this experiment was to check that.

This solution requires the various versions of HP.HPTRIM.SDK.dll for each version I was testing - TRIM7, RM81, RM83, and CM9, which I haven't 
included because of licensing. But, I'm assuming if you care enough to get here, you have access to such files yourself. You'll need to put them in the
_dependencies folder, or just remove the existing HP.HPTRIM.SDK project reference and add your one instead.

It's a .Net Framework 4.6.1 Console app that, after you supply the following:

* Record Type

* Container

* Assignee (Current Location)

* Is Enclosed (y/n)

It will try all permutations of setting Container, Assignee and IsEnclosed and output which sequences worked, and which failed.

## Example output for RM 8.3

	RecordType name or uri:> 503
	Container name or uri:> 15W/1
	Assignee name or uri:> 514
	Enclose record in container? (y|n):> n
	-> Container-> Assignee-> Enclosed
	SUCCESS >>> Container: correct -> Assignee: correct -> Enclosed: correct
	-> Container-> Enclosed-> Assignee
	SUCCESS >>> Container: correct -> Enclosed: correct -> Assignee: correct
	-> Assignee-> Container-> Enclosed
	FAILED >>> Assignee: WRONG -> Container: correct -> Enclosed: WRONG
	-> Assignee-> Enclosed-> Container
	FAILED >>> Assignee: WRONG -> Enclosed: WRONG -> Container: correct
	-> Enclosed-> Container-> Assignee
	SUCCESS >>> Enclosed: correct -> Container: correct -> Assignee: correct
	-> Enclosed-> Assignee-> Container
	FAILED >>> Enclosed: WRONG -> Assignee: WRONG -> Container: correct

## Conclusions

The results of my experiment showed that the order does matter in RM 8.3 - in that as long as Assignee is set after Container, everything is OK
(and the order of Enclosed doesn't matter).

Running this program on TRIM 7 and RM 8.1 environments showed that all permutations failed - there's an underlying issue with TRIM/RM, which I
confirmed using their own thick client. (The user would have to save an orginal "Enclosed" record, and then edit it to un-enclose and assign as
needed).
