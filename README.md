# DotnetMarlowe

Toy implementation for serializing and deserializing [Cardano](https://cardano.org/) [Marlowe](https://alpha.marlowe.iohkdev.io/doc/index.html) 
smart contracts to and from F# DU types.

This project was created for experimenting with programatically generating (financial) smart contracts for Cardano using .NET/F#
rather than having to use Haskell or Marlowe directly. It's just a toy implementation and should be treated as such..

##### Simple test runner
```
$ dotnet fsi script.fsx test
> samples/Contract_CouponBondGuaranteed.marlowe : OK
> samples/Contract_test.marlowe : OK
> samples/Contract_oraclediffs.marlowe : OK
> samples/Contract_diffs.marlowe : OK
> samples/Contract_swap.marlowe : OK
> samples/Contract_Escrow.marlowe : OK
> samples/Contract_ZeroCouponBond.marlowe : OK
> samples/Contract_EscrowWithCollateral.marlowe : OK
> samples/Contract_sample.marlowe : OK
> samples/Deposit_Sample.marlowe : OK
```

##### Parse a marlowe file and get the resulting F# object
```
$ dotnet fsi script.fsx samples/Deposit_Sample.marlowe
> Deserialization completed in: 15ms
------------------------------------------
Deposit
  (Role "Ada provider", Role "Ada provider", Token ("", ""),
   MulValue (Constant 1000000L, ConstantParam "Amount of Ada"))
```

##### Deserializing Marlowe contract into F#:

```fsharp
// raw marlowe dsl text
let sample = """
  When [
  (Case
     (Deposit
        (Role "Seller")
        (Role "Buyer")
        (Token "" "")
        (ConstantParam "Price"))
     (When [
           (Case
              (Choice
                 (ChoiceId "Everything is alright"
                    (Role "Buyer")) [
                 (Bound 0 0)]) Close)
           ,
           (Case
              (Choice
                 (ChoiceId "Report problem"
                    (Role "Buyer")) [
                 (Bound 1 1)])
              (Pay
                 (Role "Seller")
                 (Account
                    (Role "Buyer"))
                 (Token "" "")
                 (ConstantParam "Price")
                 (When [
                       (Case
                          (Choice
                             (ChoiceId "Confirm problem"
                                (Role "Seller")) [
                             (Bound 1 1)]) Close)
                       ,
                       (Case
                          (Choice
                             (ChoiceId "Dispute problem"
                                (Role "Seller")) [
                             (Bound 0 0)])
                          (When [
                                (Case
                                   (Choice
                                      (ChoiceId "Dismiss claim"
                                         (Role "Arbiter")) [
                                      (Bound 0 0)])
                                   (Pay
                                      (Role "Buyer")
                                      (Party
                                         (Role "Seller"))
                                      (Token "" "")
                                      (ConstantParam "Price") Close))
                                ,
                                (Case
                                   (Choice
                                      (ChoiceId "Confirm problem"
                                         (Role "Arbiter")) [
                                      (Bound 1 1)]) Close)] (SlotParam "Timeout for arbitrage") Close))] (SlotParam "Seller's response timeout") Close)))] (SlotParam "Buyer's dispute timeout") Close))] 123 Close
"""

// fancy pants f# typed contract object!
let myContract = sample |> dotnetmarlowe.Deserializer.DeserializeMarlowe<dotnetmarlowe.Types.Contract> 
myContract // Result:

When
  ([Case
      (Deposit
         (Role "Seller", Role "Buyer", Token ("", ""), ConstantParam "Price"),
       When
         ([Case
             (Choice
                (ChoiceId ("Everything is alright", Role "Buyer"),
                 [Bound (0L, 0L)]), Close);
           Case
             (Choice
                (ChoiceId ("Report problem", Role "Buyer"), [Bound (1L, 1L)]),
              Pay
                ((Role "Seller", Account (Role "Buyer"), Token ("", ""),
                  ConstantParam "Price"),
                 When
                   ([Case
                       (Choice
                          (ChoiceId ("Confirm problem", Role "Seller"),
                           [Bound (1L, 1L)]), Close);
                     Case
                       (Choice
                          (ChoiceId ("Dispute problem", Role "Seller"),
                           [Bound (0L, 0L)]),
                        When
                          ([Case
                              (Choice
                                 (ChoiceId ("Dismiss claim", Role "Arbiter"),
                                  [Bound (0L, 0L)]),
                               Pay
                                 ((Role "Buyer", Party (Role "Seller"),
                                   Token ("", ""), ConstantParam "Price"), Close));
                            Case
                              (Choice
                                 (ChoiceId ("Confirm problem", Role "Arbiter"),
                                  [Bound (1L, 1L)]), Close)],
                           SlotParam "Timeout for arbitrage", Close))],
                    SlotParam "Seller's response timeout", Close)))],
          SlotParam "Buyer's dispute timeout", Close))], SlotConstant 123L,
   Close)
```

##### Serializing F# DU's to Marlowe

```
myContract |> dotnetmarlowe.Serializer.serializeUnionTypeIntoMarlowe 
// result:
  When [
  (Case
     (Deposit
        (Role "Seller")
        (Role "Buyer")
        (Token "" "")
        (ConstantParam "Price"))
     (When [
           (Case
              (Choice
                 (ChoiceId "Everything is alright"
                    (Role "Buyer")) [
                 (Bound 0 0)]) Close)
           ,
           (Case
              (Choice
                 (ChoiceId "Report problem"
                    (Role "Buyer")) [
                 (Bound 1 1)])
              (Pay
                 (Role "Seller")
                 (Account
                    (Role "Buyer"))
                 (Token "" "")
                 (ConstantParam "Price")
                 (When [
                       (Case
                          (Choice
                             (ChoiceId "Confirm problem"
                                (Role "Seller")) [
                             (Bound 1 1)]) Close)
                       ,
                       (Case
                          (Choice
                             (ChoiceId "Dispute problem"
                                (Role "Seller")) [
                             (Bound 0 0)])
                          (When [
                                (Case
                                   (Choice
                                      (ChoiceId "Dismiss claim"
                                         (Role "Arbiter")) [
                                      (Bound 0 0)])
                                   (Pay
                                      (Role "Buyer")
                                      (Party
                                         (Role "Seller"))
                                      (Token "" "")
                                      (ConstantParam "Price") Close))
                                ,
                                (Case
                                   (Choice
                                      (ChoiceId "Confirm problem"
                                         (Role "Arbiter")) [
                                      (Bound 1 1)]) Close)] (SlotParam "Timeout for arbitrage") Close))] (SlotParam "Seller's response timeout") Close)))] (SlotParam "Buyer's dispute timeout") Close))] 123 Close
```
