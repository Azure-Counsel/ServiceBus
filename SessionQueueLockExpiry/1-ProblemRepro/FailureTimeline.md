====================================================================================================
                              SESSION LOCK EXPIRY FAILURE
====================================================================================================

SESSION STREAM

[M1] Debit $100 ───► [M2] Add Interest ───► [M3] Close Account

====================================================================================================

TIME     WORKER A                             WORKER B                 ACCOUNT STATE
----     --------                             --------                 -------------

00s      Acquires Session Lock                                         $1000 Open

01s      Receives M1

10s      API Call

15s      DB Retry Loop

30s      LOCK EXPIRES
         (Worker A unaware)

                                             Acquires Session Lock

31s                                         Receives M2

32s                                         Apply Interest +2%
                                             Balance = 1020          $1020 Open

33s                                         Receives M3

35s                                         Close Account            $1020 Closed

40s      Retry Succeeds

41s      Debit $100                                                 $920 Closed

====================================================================================================

EXPECTED

$1000
→ Debit $100
→ $900

$900
→ Interest +2%
→ $918

Close Account

FINAL = $918

====================================================================================================

ACTUAL

$1000
→ Interest +2%
→ $1020

Close Account

Debit $100

FINAL = $920

====================================================================================================

CORRUPTION DETECTED

✓ No Exception

✓ No Dead Letter

✓ No Failed Message

✓ No Alert

✗ Financial State Corrupted

✗ FIFO Guarantee Lost

====================================================================================================