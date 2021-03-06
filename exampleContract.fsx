#load "src/types.fs"
//#r "/media/zdx/data/Source/dotnetmarlowe/bin/Debug/net5.0/dotmarlowe.dll"

open dotnetmarlowe.Types

let myContract = 
    
    let dollarProviderRole = Role "Dollar provider"
    let adaProviderRole = Role "Ada provider"
    let adaConstantParam = ConstantParam "Amount of Ada"
    let dollarConstantParam = ConstantParam "Amount of dollars"

    Contract.When (
          [
            Case (
                Deposit(
                    adaProviderRole, 
                    adaProviderRole, 
                    Token ("", ""),
                    MulValue (
                        Constant 1000000L, 
                        adaConstantParam
                    )
                ),
                When (
                    [
                        Case(

                            Deposit(
                                dollarProviderRole, 
                                dollarProviderRole,
                                Token ("85bb65", "dollar"), 
                                dollarConstantParam),
                            Pay(
                                Payment(
                                    adaProviderRole, 
                                    Party (dollarProviderRole),
                                    Token ("", ""),
                                    MulValue (
                                        Constant 1000000L, 
                                        adaConstantParam)
                                ),
                                Pay(
                                    Payment (
                                        dollarProviderRole, 
                                        Party adaProviderRole,
                                        Token ("85bb65", "dollar"),
                                        dollarConstantParam
                                    ), 
                                    Close
                                )
                            )
                        )
                    ],
                    SlotParam "Timeout for dollar deposit", 
                    Close
                )
            )
        ],
        SlotParam "Timeout for Ada deposit", 
        Close
    )
