#load "../Utilities/Utilities.fsx"

open Utilities

let isEven =
    flip (%) 2 >> (=) 0